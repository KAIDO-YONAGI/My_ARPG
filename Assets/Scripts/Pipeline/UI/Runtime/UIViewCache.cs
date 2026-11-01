using System;
using System.Collections.Generic;
using Contracts;
using Pipeline.SO.Events;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pipeline.UI.Runtime
{
    /// <summary>
    /// 界面实例缓存：按 <see cref="CanvasToToggle"/> 懒创建界面，关闭后保留实例，
    /// 超出容量或闲置超时再销毁，避免每次开面板都重新实例化整棵 UI。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 数据结构是“字典 + 双向链表”：字典负责按界面 ID 定位，链表负责维护闲置顺序。
    /// 链表头部是最久未使用、最该被淘汰的实例；正在显示的界面不进入链表。
    /// </para>
    /// <para>
    /// 缓存只负责实例生命周期，不持有业务数据。界面关闭后业务状态仍由各 Manager 保存，
    /// 重新打开时由 View 通过管理器的 <c>State</c> 拉取最新状态。
    /// </para>
    /// </remarks>
    public sealed class UIViewCache : IDisposable
    {
        /// <summary>单个界面的注册信息与运行期引用。</summary>
        private sealed class Entry
        {
            public CanvasToToggle Id;
            public CachedUIView Prefab;
            public GameObject Anchor;
            public CachedUIView View;
            public ToggleCanvasEventSO ToggleEvent;
            public Action<bool> Toggle;
            public Action Focus;
            public Action Closed;
            public LinkedListNode<Entry> IdleNode;
            public float ClosedAt;
            public long LastUsedSequence;
        }

        private readonly Dictionary<CanvasToToggle, Entry> entries = new();
        private readonly LinkedList<Entry> idle = new();
        private readonly Transform owner;
        private readonly int maxIdleViews;
        private readonly float idleTimeoutSeconds;
        private readonly Action<CanvasToToggle, bool, bool, bool> reportState;
        private readonly Func<CanvasToToggle, bool, int> getOrder;
        private bool connected;
        private long usageSequence;

        /// <summary>当前闲置实例数量，供调试和 Inspector 查看。</summary>
        public int IdleCount => idle.Count;

        /// <summary>当前已创建且存活的界面实例数量（含正在显示的实例）。</summary>
        public int InstanceCount
        {
            get
            {
                int count = 0;
                foreach (var entry in entries.Values)
                    if (entry.View != null) count++;
                return count;
            }
        }

        /// <summary>
        /// 建立缓存并校验注册项。预制体、挂载点和开关事件缺一不可，
        /// 且开关事件上的枚举必须与预制体的 <see cref="CachedUIView.CanvasId"/> 一致。
        /// </summary>
        /// <param name="bindings">界面注册表：预制体 + 场景挂载点。</param>
        /// <param name="owner">缓存根节点，用于判断界面当前是否可以打开。</param>
        /// <param name="maxIdleViews">最多保留的闲置实例数，0 表示关闭即销毁。</param>
        /// <param name="idleTimeoutSeconds">闲置超时秒数，0 表示关闭后下一帧淘汰。</param>
        /// <param name="reportState">向 UIManager 上报显隐、ESC 与输入阻断状态。</param>
        /// <param name="getOrder">查询画布排序值。</param>
        public UIViewCache(
            IEnumerable<CachedViewBinding> bindings,
            Transform owner,
            int maxIdleViews,
            float idleTimeoutSeconds,
            Action<CanvasToToggle, bool, bool, bool> reportState,
            Func<CanvasToToggle, bool, int> getOrder)
        {
            this.owner = owner;
            this.maxIdleViews = Math.Max(0, maxIdleViews);
            this.idleTimeoutSeconds = Mathf.Max(0f, idleTimeoutSeconds);
            this.reportState = reportState;
            this.getOrder = getOrder;

            if (bindings == null) return;
            foreach (var binding in bindings)
            {
                var prefab = binding?.prefab;
                if (prefab == null || binding.anchor == null ||
                    prefab.CanvasId == CanvasToToggle.Default || prefab.ToggleEvent == null ||
                    prefab.ToggleEvent.canvasToToggle != prefab.CanvasId)
                {
#if UNITY_EDITOR
                    Debug.LogError("UI 缓存注册失败：Prefab、Anchor 或匹配的开关事件缺失，或使用了 Default ID.", prefab);
#endif
                    continue;
                }

                // 缓存只接受预制体资产，不接受场景里已经摆好的实例，否则会重复创建或改坏场景对象。
                if (prefab.gameObject.scene.IsValid() || !prefab.enabled)
                {
#if UNITY_EDITOR
                    Debug.LogError("UI 缓存只接受启用了 View 组件的预制体资产。", prefab);
#endif
                    continue;
                }

                var id = prefab.CanvasId;
                if (entries.ContainsKey(id))
                {
#if UNITY_EDITOR
                    Debug.LogError($"UI 缓存重复注册：{id}。", prefab);
#endif
                    continue;
                }

                var entry = new Entry
                {
                    Id = id,
                    Prefab = prefab,
                    Anchor = binding.anchor,
                    ToggleEvent = prefab.ToggleEvent
                };
                // 对话和任务由业务入口准备数据后打开；ESC、互斥和切场仍统一走关闭事件。
                entry.Toggle = state =>
                {
                    if (!state || entry.Prefab.OpenOnToggleEvent)
                        SetOpen(entry, state);
                };
                entry.Focus = () => RefreshOrder(entry);
                entry.Closed = () => OnClosed(entry);
                entries.Add(id, entry);
            }
        }

        /// <summary>按界面 ID 取开关事件，供 UIManager 统一分发按键、ESC 和外部请求。</summary>
        public ToggleCanvasEventSO GetToggleEvent(CanvasToToggle id)
        {
            return entries.TryGetValue(id, out var entry) ? entry.ToggleEvent : null;
        }

        /// <summary>按界面 ID 取注册的预制体，供需要读取预制体初始数据的业务使用。</summary>
        public CachedUIView GetPrefab(CanvasToToggle id)
        {
            return entries.TryGetValue(id, out var entry) ? entry.Prefab : null;
        }

        /// <summary>订阅所有开关事件与焦点事件；重复调用无副作用。</summary>
        public void Connect()
        {
            if (connected) return;
            connected = true;
            foreach (var entry in entries.Values)
            {
                entry.ToggleEvent.toggleCanvasEvent += entry.Toggle;
                entry.ToggleEvent.focusEvent += entry.Focus;
            }
        }

        /// <summary>退订所有事件；与 <see cref="Connect"/> 成对调用。</summary>
        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            foreach (var entry in entries.Values)
            {
                entry.ToggleEvent.toggleCanvasEvent -= entry.Toggle;
                entry.ToggleEvent.focusEvent -= entry.Focus;
            }
        }

        /// <summary>按代码请求打开界面；界面未注册时返回 false。</summary>
        public bool OpenCanvas(CanvasToToggle id)
        {
            if (!entries.TryGetValue(id, out var entry)) return false;
            return SetOpen(entry, true);
        }

        /// <summary>按代码请求关闭界面；界面未注册或本来没开时静默返回。</summary>
        public void CloseCanvas(CanvasToToggle id)
        {
            if (entries.TryGetValue(id, out var entry))
                SetOpen(entry, false);
        }

        /// <summary>查询界面是否处于打开状态；实例尚未创建时视为关闭。</summary>
        public bool IsCanvasOpen(CanvasToToggle id)
        {
            return entries.TryGetValue(id, out var entry) &&
                   entry.View != null && entry.View.IsOpen;
        }

        /// <summary>
        /// 记录一次使用，把界面移到“最近使用”一端。
        /// UIManager 在画布打开事件后调用，保证闲置淘汰顺序反映真实使用顺序。
        /// </summary>
        public void Touch(CanvasToToggle id)
        {
            if (entries.TryGetValue(id, out var entry) && entry.View != null && entry.View.IsOpen)
            {
                RemoveIdle(entry);
                entry.LastUsedSequence = ++usageSequence;
            }
        }

        /// <summary>
        /// 每帧按关闭时刻检查闲置超时。链表按最近使用顺序排列，需要逐项检查。
        /// 销毁会修改链表，所以循环里先保存下一个节点。
        /// </summary>
        public void Update(float now)
        {
            var node = idle.First;
            while (node != null)
            {
                var next = node.Next;
                if (now - node.Value.ClosedAt >= idleTimeoutSeconds)
                    DestroyInstance(node.Value);
                node = next;
            }
        }

        /// <summary>
        /// 打开或关闭一个界面。
        /// 打开时按需实例化并挂到场景 Anchor 下；关闭时只隐藏，不销毁。
        /// </summary>
        private bool SetOpen(Entry entry, bool state)
        {
            if (!state)
            {
                if (entry.View != null) entry.View.Close();
                return true;
            }

            // 挂载点或缓存根被停用时不能打开，否则界面会挂在未激活层级下，看起来像“没反应”。
            if (owner == null || !owner.gameObject.activeInHierarchy || entry.Anchor == null ||
                !entry.Anchor.activeInHierarchy)
                return false;

            try
            {
                CachedUIView candidate = entry.View != null ? entry.View : entry.Prefab;
                if (!candidate.CanOpenFromCache()) return false;

                if (entry.View == null)
                {
                    // 直接建在挂载点下并立即停用，等 Open 时再激活，避免半初始化的界面参与布局。
                    entry.View = Object.Instantiate(entry.Prefab, entry.Anchor.transform, false);
                    entry.View.gameObject.SetActive(false);
                    entry.View.Closed += entry.Closed;
                }

                if (!entry.View.Open())
                {
                    // CanOpen 失败：如果实例还是闲置状态，顺手清掉，避免留下无法打开的缓存。
                    if (entry.IdleNode == null)
                        DestroyInstance(entry);
                    return false;
                }

                RemoveIdle(entry);
                reportState(entry.Id, true,
                    entry.View.CloseOnEscape, entry.View.BlocksGlobalInput);
                RefreshOrder(entry);
                return true;
            }
            catch (Exception exception)
            {
                DestroyInstance(entry);
                reportState(entry.Id, false, true, false);
#if UNITY_EDITOR
                Debug.LogException(exception);
#endif
                return false;
            }
        }

        /// <summary>把已打开界面重新排到最前；关闭中的实例没有排序意义。</summary>
        private void RefreshOrder(Entry entry)
        {
            if (entry.View != null && entry.View.IsOpen)
                entry.View.SetOrder(getOrder(entry.Id, true));
        }

        /// <summary>
        /// 界面关闭回调：上报状态、按最近使用顺序插入闲置链表，
        /// 再按容量淘汰最久未使用的实例。正在显示的界面不会进入这条路径。
        /// </summary>
        private void OnClosed(Entry entry)
        {
            reportState(entry.Id, false,
                entry.View.CloseOnEscape, entry.View.BlocksGlobalInput);
            RemoveIdle(entry);
            entry.ClosedAt = Time.unscaledTime;

            // 链表按 LastUsedSequence 升序排列，关闭时间只用于超时判断。
            var node = idle.First;
            while (node != null && node.Value.LastUsedSequence <= entry.LastUsedSequence)
                node = node.Next;
            entry.IdleNode = node == null
                ? idle.AddLast(entry)
                : idle.AddBefore(node, entry);
            while (idle.Count > maxIdleViews)
                DestroyInstance(idle.First.Value);
        }

        /// <summary>从闲置链表移除；本来就不在链表里时直接返回。</summary>
        private void RemoveIdle(Entry entry)
        {
            if (entry.IdleNode == null) return;
            idle.Remove(entry.IdleNode);
            entry.IdleNode = null;
        }

        /// <summary>销毁实例并清空 Entry 上的运行时引用，保留预制体和挂载点以便下次重建。</summary>
        private void DestroyInstance(Entry entry)
        {
            RemoveIdle(entry);
            if (entry.View == null) return;
            entry.View.BeforeCacheDestroy();
            entry.View.Closed -= entry.Closed;
            Object.Destroy(entry.View.gameObject);
            entry.View = null;
        }

        /// <summary>关闭所有已打开界面；闲置实例保持不动。</summary>
        public void CloseAll()
        {
            foreach (var entry in entries.Values)
                if (entry.View != null) entry.View.Close();
        }

        /// <summary>立即销毁全部闲置实例，切场景或手动清理时使用。</summary>
        public void ClearIdle()
        {
            while (idle.First != null)
                DestroyInstance(idle.First.Value);
        }

        /// <summary>释放缓存：退订事件、关闭界面、销毁闲置实例。</summary>
        public void Dispose()
        {
            Disconnect();
            CloseAll();
            ClearIdle();
        }
    }
}
