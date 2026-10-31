using System;
using System.Collections.Generic;
using Contracts;
using Pipeline.SO.Events;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pipeline.UI.Runtime
{
    /// <summary>按界面 ID 延迟创建单个实例；双向链表仅保存已关闭的实例。</summary>
    public sealed class UIViewCache : IDisposable
    {
        private sealed class Entry
        {
            public CachedUIView Prefab;
            public CachedUIView View;
            public ToggleCanvasEventSO Event;
            public Action<bool> Toggle;
            public Action Focus;
            public Action Closed;
            public LinkedListNode<Entry> IdleNode;
        }

        private readonly Dictionary<CanvasToToggle, Entry> entries = new();
        private readonly LinkedList<Entry> idle = new();
        private readonly Transform root;
        private readonly GameObject stagingRoot;
        private readonly int maxIdleViews;
        private readonly Action<CanvasToToggle, bool, bool, bool> reportState;
        private readonly Func<CanvasToToggle, bool, int> getOrder;
        private bool connected;

        public int IdleCount => idle.Count;

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

        public UIViewCache(
            IEnumerable<CachedUIView> prefabs,
            Transform root,
            int maxIdleViews,
            Action<CanvasToToggle, bool, bool, bool> reportState,
            Func<CanvasToToggle, bool, int> getOrder)
        {
            this.root = root;
            this.maxIdleViews = Math.Max(0, maxIdleViews);
            this.reportState = reportState;
            this.getOrder = getOrder;

            // 在停用的父节点下克隆，避免尚未绑定数据就触发子控件 OnEnable。
            stagingRoot = new GameObject("InactiveUIStaging");
            stagingRoot.SetActive(false);
            stagingRoot.transform.SetParent(root, false);

            if (prefabs == null) return;
            foreach (var prefab in prefabs)
            {
                if (prefab == null || prefab.ToggleEvent == null ||
                    prefab.ToggleEvent.canvasToToggle == CanvasToToggle.Default)
                {
                    Debug.LogError("UI 缓存注册失败：Prefab 或开关事件缺失，或使用了 Default ID。", prefab);
                    continue;
                }

                if (prefab.gameObject.scene.IsValid() || !prefab.enabled)
                {
                    Debug.LogError("UI 缓存只接受启用了 View 组件的预制体资产。", prefab);
                    continue;
                }

                var id = prefab.ToggleEvent.canvasToToggle;
                if (entries.ContainsKey(id))
                {
                    Debug.LogError($"UI 缓存重复注册：{id}。", prefab);
                    continue;
                }

                var entry = new Entry { Prefab = prefab, Event = prefab.ToggleEvent };
                entry.Toggle = state => SetOpen(entry, state);
                entry.Focus = () => RefreshOrder(entry);
                entry.Closed = () => OnClosed(entry);
                entries.Add(id, entry);
            }
        }

        public ToggleCanvasEventSO GetToggleEvent(CanvasToToggle id)
        {
            return entries.TryGetValue(id, out var entry) ? entry.Event : null;
        }

        public void Connect()
        {
            if (connected) return;
            connected = true;
            foreach (var entry in entries.Values)
            {
                entry.Event.toggleCanvasEvent += entry.Toggle;
                entry.Event.focusEvent += entry.Focus;
            }
        }

        public void Disconnect()
        {
            if (!connected) return;
            connected = false;
            foreach (var entry in entries.Values)
            {
                entry.Event.toggleCanvasEvent -= entry.Toggle;
                entry.Event.focusEvent -= entry.Focus;
            }
        }

        private void SetOpen(Entry entry, bool state)
        {
            if (!state)
            {
                if (entry.View != null) entry.View.Close();
                return;
            }

            if (root == null || !root.gameObject.activeInHierarchy) return;
            RemoveIdle(entry);
            try
            {
                if (entry.View == null)
                {
                    entry.View = Object.Instantiate(entry.Prefab, stagingRoot.transform, false);
                    entry.View.gameObject.SetActive(false);
                    entry.View.transform.SetParent(root, false);
                    entry.View.Closed += entry.Closed;
                }

                entry.View.Open();
                reportState(entry.Event.canvasToToggle, true,
                    entry.View.CloseOnEscape, entry.View.BlocksGlobalInput);
                RefreshOrder(entry);
            }
            catch (Exception exception)
            {
                DestroyInstance(entry);
                reportState(entry.Event.canvasToToggle, false, true, false);
                Debug.LogException(exception);
            }
        }

        private void RefreshOrder(Entry entry)
        {
            if (entry.View != null && entry.View.IsOpen)
                entry.View.SetOrder(getOrder(entry.Event.canvasToToggle, true));
        }

        private void OnClosed(Entry entry)
        {
            reportState(entry.Event.canvasToToggle, false,
                entry.View.CloseOnEscape, entry.View.BlocksGlobalInput);
            RemoveIdle(entry);
            entry.IdleNode = idle.AddLast(entry);
            while (idle.Count > maxIdleViews)
                DestroyInstance(idle.First.Value);
        }

        private void RemoveIdle(Entry entry)
        {
            if (entry.IdleNode == null) return;
            idle.Remove(entry.IdleNode);
            entry.IdleNode = null;
        }

        private void DestroyInstance(Entry entry)
        {
            RemoveIdle(entry);
            if (entry.View == null) return;
            entry.View.Closed -= entry.Closed;
            Object.Destroy(entry.View.gameObject);
            entry.View = null;
        }

        public void CloseAll()
        {
            foreach (var entry in entries.Values)
                if (entry.View != null) entry.View.Close();
        }

        public void ClearIdle()
        {
            while (idle.First != null)
                DestroyInstance(idle.First.Value);
        }

        public void Dispose()
        {
            Disconnect();
            CloseAll();
            ClearIdle();
            if (stagingRoot != null) Object.Destroy(stagingRoot);
        }
    }
}
