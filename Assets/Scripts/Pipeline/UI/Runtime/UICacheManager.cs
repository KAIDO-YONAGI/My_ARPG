using System;
using System.Collections.Generic;
using Contracts;
using Pipeline.SO.Events;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;

namespace Pipeline.UI.Runtime
{
    /// <summary>
    /// 一条界面注册项：预制体资产 + 常驻场景中的挂载点。
    /// 预制体提供界面内容，挂载点只决定实例创建在哪个父节点下。
    /// </summary>
    [Serializable]
    public sealed class CachedViewBinding
    {
        [Tooltip("要缓存的界面预制体，必须挂有 CachedUIView 派生组件。")]
        public CachedUIView prefab;

        [Tooltip("常驻场景里的挂载点，界面实例会以它为父节点创建。")]
        public GameObject anchor;
    }

    /// <summary>
    /// 界面缓存的场景入口：持有注册表，创建 <see cref="UIViewCache"/>，
    /// 并把画布开关事件与 UIManager 的状态上报连接起来。
    /// </summary>
    /// <remarks>
    /// 本组件挂在常驻场景的 RuntimeViews 下。它不直接处理输入，
    /// 输入由 UIManager 统一收集后再通过 <see cref="OpenCanvas"/> / <see cref="CloseCanvas"/> 驱动缓存。
    /// </remarks>
    public sealed class UICacheManager : MonoBehaviour
    {
        [Header("界面注册表")]
        [Tooltip("每项登记一个界面预制体和它的场景挂载点；同一 CanvasToToggle 只能登记一次。")]
        [SerializeField] private List<CachedViewBinding> views = new();

        [Header("事件与缓存策略")]
        [Tooltip("画布打开后发布，用于刷新最近使用顺序。")]
        [SerializeField] private CanvasOpenedEventSO canvasOpenedEvent;

        [Tooltip("最多保留的闲置界面实例数，0 表示关闭即销毁。")]
        [Min(0)] [SerializeField] private int maxIdleViews = 3;

        [Tooltip("闲置超过多少秒后销毁实例，0 表示关闭后下一帧销毁。")]
        [Min(0f)] [SerializeField] private float idleTimeoutSeconds = 120f;

        private UIViewCache cache;

        /// <summary>当前场景中的缓存入口；场景切换时可能短暂为空。</summary>
        public static UICacheManager Instance { get; private set; }

        /// <summary>已创建的界面实例数量（含正在显示的界面）。</summary>
        public int InstanceCount => cache?.InstanceCount ?? 0;

        /// <summary>当前闲置可复用的界面实例数量。</summary>
        public int IdleCount => cache?.IdleCount ?? 0;

        private void Awake()
        {
            Instance = this;
            cache = new UIViewCache(
                views,
                transform,
                maxIdleViews,
                idleTimeoutSeconds,
                UIManager.Report,
                GetCanvasOrder);
        }

        private void OnEnable()
        {
            Instance = this;
            cache.Connect();
            if (canvasOpenedEvent != null)
                canvasOpenedEvent.CanvasOpened += OnCanvasOpened;
        }

        private void Update()
        {
            // 用不受时间缩放影响的时钟，暂停时也能按真实时间淘汰闲置界面。
            cache.Update(Time.unscaledTime);
        }

        private void OnDisable()
        {
            if (canvasOpenedEvent != null)
                canvasOpenedEvent.CanvasOpened -= OnCanvasOpened;
            cache?.CloseAll();
            cache?.Disconnect();
        }

        private void OnDestroy()
        {
            cache?.Dispose();
            cache = null;
            if (Instance == this) Instance = null;
        }

        /// <summary>取某个界面的开关事件；未注册时返回 null。</summary>
        public ToggleCanvasEventSO GetToggleEvent(CanvasToToggle canvas)
        {
            return cache?.GetToggleEvent(canvas);
        }

        /// <summary>取某个界面的预制体资产；未注册时返回 null。</summary>
        public CachedUIView GetPrefab(CanvasToToggle canvas)
        {
            return cache?.GetPrefab(canvas);
        }

        /// <summary>按代码打开界面；缓存尚未创建时返回 false。</summary>
        public bool OpenCanvas(CanvasToToggle canvas)
        {
            return cache != null && cache.OpenCanvas(canvas);
        }

        /// <summary>按代码关闭界面；界面本来没开时静默返回。</summary>
        public void CloseCanvas(CanvasToToggle canvas)
        {
            cache?.CloseCanvas(canvas);
        }

        /// <summary>查询界面是否处于打开状态。</summary>
        public bool IsCanvasOpen(CanvasToToggle canvas)
        {
            return cache != null && cache.IsCanvasOpen(canvas);
        }

        /// <summary>关闭全部已打开界面，切场景前调用。</summary>
        public void CloseAll()
        {
            cache?.CloseAll();
        }

        /// <summary>立即销毁全部闲置实例，切场景或手动清理时调用。</summary>
        public void ClearIdle()
        {
            cache?.ClearIdle();
        }

        /// <summary>向 UIManager 查询画布排序；UIManager 尚未就绪时回落到默认层级。</summary>
        private int GetCanvasOrder(CanvasToToggle canvas, bool state)
        {
            return UIManager.Instance != null
                ? UIManager.Instance.GetCanvasOrder(canvas, state)
                : UIManager.DefaultOrder;
        }

        /// <summary>画布打开后刷新最近使用顺序，供闲置淘汰使用。</summary>
        private void OnCanvasOpened(CanvasToToggle canvas)
        {
            cache?.Touch(canvas);
        }
    }
}
