using System;
using System.Collections.Generic;
using Contracts;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pipeline.UI.SystemCanvasManagers
{
    public class UIManager : YSingleton<UIManager>
    {
        // 常量保留转发，避免破坏 ICanvasManager.RefreshCanvaOrder 对 UIManager.DefaultOrder 的引用。
        public const int FocusOrder = CanvasFocusStack.FocusOrder;
        public const int DefaultOrder = CanvasFocusStack.DefaultOrder;

        [Header("Events")] [SerializeField] private SceneLoadEventSO loadEventSO;

        [Tooltip(
            "场景中已有画布的开关事件。按需创建的界面在 Prefab Cache 中登记；两种登记均支持热键、ESC、切场复位和互斥关闭。")]
        [SerializeField]
        private List<ToggleCanvasEventSO> toggleCanvasEvents;

        [SerializeField]
        [Tooltip(
            "配置通过按键进入通用切换流程的画布。每项将一个 CanvasToToggle 映射到 InputActionReference；GameOver、SaveLoad 等没有按键的画布可以不配置，仍可通过 RequestCanvasToggle 处理外部请求。")]
        private List<CanvasInputBinding> inputBindings;

        [Tooltip("互斥面板列表：列在此处的面板互相互斥——任一打开时自动关闭其它已打开的互斥面板；未列出的可与任意面板共存")] [SerializeField]
        private List<CanvasToToggle> mutexCanvases = new List<CanvasToToggle>();

        [Header("Prefab Cache")]
        [Tooltip("按需创建的界面预制体；以各自 ToggleEvent 的 CanvasToToggle 作为唯一 ID。")]
        [SerializeField] private List<CachedUIView> cachedViewPrefabs = new();
        [SerializeField] private Transform cachedViewRoot;
        [Tooltip("最多保留多少个已关闭的界面实例；0 表示关闭即销毁，打开的界面不计入。")]
        [Min(0)] [SerializeField] private int maxIdleViews = 3;

        private UIViewCache viewCache;
        public int CachedViewCount => viewCache?.InstanceCount ?? 0;
        public int IdleViewCount => viewCache?.IdleCount ?? 0;

        // 画布焦点栈：纯 C# 逻辑（open-order 链表、focus、sortingOrder 计算）。
        private readonly CanvasFocusStack focusStack = new();

        // 各画布上报的阻塞声明（ReportCanvasState 时登记）
        private readonly Dictionary<CanvasToToggle, bool> canvasBlocksInput = new();
        private readonly Dictionary<CanvasToToggle, bool> canvasCloseOnEscape = new();

        // 外部（代码/按键）输入合并到这里
        private readonly Dictionary<CanvasToToggle, bool> inputState = new();

        protected override void OnSingletonInitialized()
        {
            //用枚举类初始化inputState
            foreach (CanvasToToggle canvas in Enum.GetValues(typeof(CanvasToToggle)))
            {
                inputState[canvas] = false;
            }

            // 接线：纯逻辑栈通过回调驱动 SO 事件，保持自身不依赖 ToggleCanvasEventSO。
            focusStack.OnCanvasToggleRequested = RaiseCanvasEvent;
            focusStack.OnFocusRefreshRequested = RaiseFocusEvent;
            viewCache = new UIViewCache(cachedViewPrefabs,
                cachedViewRoot != null ? cachedViewRoot : transform,
                maxIdleViews, ReportCanvasState, GetCanvasOrder);
        }

        private void OnEnable()
        {
            viewCache?.Connect();
            loadEventSO.LoadRequestEvent += OnLoadScene;

            foreach (var binding in inputBindings)
            {
                binding.action.action.Enable();
            }
        }

        private void OnDisable()
        {
            viewCache?.CloseAll();
            viewCache?.Disconnect();
            loadEventSO.LoadRequestEvent -= OnLoadScene;

            foreach (var binding in inputBindings)
            {
                binding.action.action.Disable();
            }
        }

        protected override void OnDestroy()
        {
            viewCache?.Dispose();
            viewCache = null;
            base.OnDestroy();
        }

        [ContextMenu("UI Cache/Clear Idle Views")]
        public void ClearIdleViews()
        {
            viewCache?.ClearIdle();
        }

        private void OnLoadScene(List<GameSceneSO> arg0, Vector3 arg1, bool arg2)
            //UIManager作为跨场景持久单例，不会随场景卸载而disable，因此需要订阅场景加载事件来主动重置画布状态。
            //本处理器在 SceneChanger.RequestSceneLoad 的广播段内同步执行，先于其切换流程，
            //面板因此在过渡动画和旧场景卸载前被关闭。
        {
            ResetCanvases();
            ClearIdleViews();
        }

        private void Update()
        {
            ToggleCanvas();
        }

        public void HandleFocus(CanvasToToggle canvas) //拖拽脚本的输入，用于完成focus调整
        {
            focusStack.HandleFocus(canvas);
        }

        /// <summary>
        /// 请求切换或聚焦画布。
        /// 有按键绑定的画布按 inputBindings 顺序处理；没有按键绑定的画布也会在
        /// ToggleCanvas 的外部请求分支中被消费。
        /// </summary>
        public void RequestCanvasToggle(CanvasToToggle canvas)
        {
            if (!inputState.ContainsKey(canvas))
            {
                return;
            }

            inputState[canvas] = true;
        }

        /// <summary>
        /// 直接关闭指定画布，不依赖 inputBindings，也不受 ESC 关闭规则限制。
        /// </summary>
        public void RequestCanvasClose(CanvasToToggle canvas)
        {
            focusStack.RequestClose(canvas);
        }

        /// <summary>
        /// 状态上报的安全入口：UIManager 尚未完成单例注册（各单例 Awake 顺序无保证）时静默跳过。
        /// 外部上报一律走这里；跳过的初始 false 上报无副作用（焦点栈默认全关）。
        /// </summary>
        public static void Report(
            CanvasToToggle canvas,
            bool state,
            bool closeOnEscape = true,
            bool blocksGlobalInput = false)
        {
            if (Instance != null)
            {
                Instance.ReportCanvasState(canvas, state, closeOnEscape, blocksGlobalInput);
            }
        }

        // 状态回调：画布报告真实的开启或关闭状态；互斥由本组件的 mutexCanvases 列表解析，阻塞声明由面板传入。
        // 仅由静态 Report 调用，保证 Instance 判空集中在唯一一处。
        private void ReportCanvasState(
            CanvasToToggle canvas,
            bool state,
            bool closeOnEscape = true,
            bool blocksGlobalInput = false)
        {
            if (canvas == CanvasToToggle.Default)
            {
                return;
            }

            canvasCloseOnEscape[canvas] = closeOnEscape;
            canvasBlocksInput[canvas] = blocksGlobalInput;

            // 互斥：开启时自动关闭其它已打开的互斥面板。
            if (state && IsMutexCanvas(canvas))
            {
                CloseOtherMutexCanvases(canvas);
            }

            focusStack.ReportState(canvas, state);
        }

        public bool IsCanvasFocused(CanvasToToggle canvas)
        {
            return focusStack.IsFocused(canvas);
        }

        // 根据 open-order 链表计算排序优先级（转发给焦点栈）。被 ICanvasManager.RefreshCanvaOrder 调用。
        public int GetCanvasOrder(CanvasToToggle canvas, bool state)
        {
            return focusStack.GetCanvasOrder(canvas, state);
        }

        private void CloseOtherMutexCanvases(CanvasToToggle opening)
        {
            // 遍历副本：RequestClose 会经由面板回调修改焦点栈。
            foreach (CanvasToToggle openCanvas in focusStack.GetOpenCanvases())
            {
                // 仅关闭可关闭面板：对“仅上报面板”发出无效关闭会导致焦点栈与真实显隐状态错位。
                if (openCanvas != opening && IsMutexCanvas(openCanvas) && IsClosableCanvas(openCanvas))
                {
                    focusStack.RequestClose(openCanvas);
                }
            }
        }

        // 是否为互斥面板：列在 mutexCanvases 里即参与互斥，未列出则可与任意面板共存。
        private bool IsMutexCanvas(CanvasToToggle canvas)
        {
            return mutexCanvases != null && mutexCanvases.Contains(canvas);
        }

        // 优先查预制体注册表，再查场景画布列表；开关、焦点与可关闭判定共用此入口。
        private ToggleCanvasEventSO FindToggleEvent(CanvasToToggle canvas)
        {
            var cachedEvent = viewCache?.GetToggleEvent(canvas);
            if (cachedEvent != null) return cachedEvent;

            foreach (var eventSO in toggleCanvasEvents)
            {
                if (eventSO != null && eventSO.canvasToToggle == canvas)
                    return eventSO;
            }

            return null;
        }

        // 任一注册表中有对应开关事件，即可由 UIManager 主动控制。
        private bool IsClosableCanvas(CanvasToToggle canvas)
        {
            return FindToggleEvent(canvas) != null;
        }

        // 是否有"阻塞全局输入"的画布处于打开状态（按焦点栈开放列表推导，避免计数漂移）。
        private bool IsGlobalInputBlocked()
        {
            foreach (CanvasToToggle openCanvas in focusStack.GetOpenCanvases())
            {
                if (canvasBlocksInput.TryGetValue(openCanvas, out bool blocks) && blocks)
                {
                    return true;
                }
            }

            return false;
        }

        private void ToggleCanvas()
        {
            CollectInputBindingRequests();

            // 阻塞画布打开时，所有切换请求都在这里被消费，避免请求残留到下一帧。
            if (IsGlobalInputBlocked())
            {
                ResetInputState();
                return;
            }

            // ESC 是独立语义：有可关闭的顶层画布时关闭它，否则打开 ESC 菜单。
            if (HandleEscapeRequest())
            {
                return;
            }

            // 普通按键请求和外部 RequestCanvasToggle 请求共用同一条焦点栈处理路径。
            HandleCanvasToggleRequest();
            ResetInputState();
        }

        /// <summary>
        /// 将本帧 inputBindings 中的按键状态合并到统一请求表。
        /// 外部 RequestCanvasToggle 请求已经提前写入 inputState，不会被这里覆盖。
        /// </summary>
        private void CollectInputBindingRequests()
        {
            foreach (var binding in inputBindings)
            {
                bool pressed = binding.action.action.WasPressedThisFrame();
                inputState[binding.canvas] = inputState[binding.canvas] || pressed;
            }
        }

        /// <summary>
        /// 处理 ESC 请求。返回 true 表示本帧已经处理完毕，调用方不应继续处理其它画布请求。
        /// </summary>
        private bool HandleEscapeRequest()
        {
            if (!inputState[CanvasToToggle.ESC])
            {
                return false;
            }

            CanvasToToggle top = focusStack.LastOpenCanvas;
            bool canCloseTop =
                top != CanvasToToggle.Default &&
                IsClosableCanvas(top) &&
                (!canvasCloseOnEscape.TryGetValue(top, out bool closeOnEscape) || closeOnEscape);

            if (canCloseTop)
            {
                focusStack.HandleESCOrCloseTop();
            }
            else
            {
                focusStack.HandleESCOrOpen();
            }

            ResetInputState();
            return true;
        }

        /// <summary>
        /// 处理普通画布切换请求。
        /// 先按 inputBindings 顺序处理按键绑定，再补充处理未配置按键但由
        /// RequestCanvasToggle 写入的外部请求，避免 GameOver、SaveLoad 等画布被过滤。
        /// </summary>
        private void HandleCanvasToggleRequest()
        {
            CanvasToToggle requested = FindRequestedCanvas();
            if (requested != CanvasToToggle.Default)
            {
                focusStack.ApplyFocusChange(requested);
            }
        }

        private CanvasToToggle FindRequestedCanvas()
        {
            // 有按键绑定的画布保持 Inspector 列表顺序，保证多个输入同帧触发时行为稳定。
            foreach (var binding in inputBindings)
            {
                if (binding.canvas == CanvasToToggle.ESC)
                {
                    continue;
                }

                if (inputState[binding.canvas])
                {
                    return binding.canvas;
                }
            }

            // 外部请求不要求存在 inputBinding；按枚举顺序消费未绑定画布的第一个请求。
            foreach (CanvasToToggle canvas in Enum.GetValues(typeof(CanvasToToggle)))
            {
                if (canvas == CanvasToToggle.ESC || HasInputBinding(canvas))
                {
                    continue;
                }

                if (inputState[canvas])
                {
                    return canvas;
                }
            }

            return CanvasToToggle.Default;
        }

        private bool HasInputBinding(CanvasToToggle canvas)
        {
            foreach (var binding in inputBindings)
            {
                if (binding.canvas == canvas)
                {
                    return true;
                }
            }

            return false;
        }

        private void RaiseCanvasEvent(CanvasToToggle target, bool state)
        {
            FindToggleEvent(target)?.RaiseToggleCanvasEvent(state);
        }

        private void RaiseFocusEvent(CanvasToToggle target)
        {
            FindToggleEvent(target)?.RaiseFocusEvent();
        }

        private void ResetInputState()
        {
            var keys = new List<CanvasToToggle>(inputState.Keys);
            foreach (var key in keys)
            {
                inputState[key] = false;
            }
        }

        private void ResetCanvases()
        {
            focusStack.Clear();

            foreach (var eventSO in toggleCanvasEvents)
            {
                eventSO.RaiseToggleCanvasEvent(false);
            }

            viewCache?.CloseAll();

            ResetInputState();
        }
    }

    [Serializable]
    public class CanvasInputBinding
    {
        public CanvasToToggle canvas;
        public InputActionReference action;
    }
}
