using System;
using System.Collections.Generic;
using Contracts.Base;
using Contracts.Enums;
using Pipeline.UI.Runtime;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pipeline.UI.RedDot
{
    /// <summary>
    /// 待办红点的树结构与待办导航。持有根节点、画布分支、导航游标与结构操作，
    /// 叶子计数变化时沿父链向上重算各层表现。
    /// </summary>
    /// <remarks>
    /// 树只保存结构与计数，业务条件由注册叶子的所属域判断。
    /// OpenMenu 显示根红点，聚合面板入口显示各画布分支红点，反引号键按画布顺序
    /// 逐个打开或聚焦有待办的画布。界面缓存淘汰只影响表现引用，计数继续保留。
    /// </remarks>
    public sealed class RedDotManager : YSingleton<RedDotManager>
    {
        /// <summary>待办导航的输入引用，场景里指向 UI/RedDotNavigate 动作。</summary>
        [Tooltip("待办导航按键（键盘反引号，即 ~ 键）。按下后按画布顺序打开或聚焦有待办的界面。")]
        [SerializeField]
        private InputActionReference navigateAction;

        // 画布身份到分支的映射；分支在所属域登记时建立。
        private readonly Dictionary<CanvasType, RedDotBranchNode> canvasBranches = new();

        // 所属域登记的画布可用性回调；未登记表示可以直接打开。
        private readonly Dictionary<CanvasType, Func<bool>> canvasGates = new();

        // 当前绑定的画布红点视图，分支建立后自动补绑。
        private readonly Dictionary<CanvasType, RedDotView> canvasViews = new();

        // 待办导航顺序，取值来源是聚合面板已配置的画布列表。
        private readonly List<CanvasType> navigationOrder = new();

        // 树根，OnSingletonInitialized 创建；画布分支与业务叶子都挂在它下面。
        private RedDotBranchNode root;

        // OpenMenu 的根红点视图；界面重建后由聚合面板重新绑定。
        private RedDotView rootView;

        // 上次成功定位的画布，用于决定下一次从它之后继续找。
        private CanvasType lastNavigatedCanvas = CanvasType.Default;

        // 已发出、尚未确认打开结果的导航目标；Default 表示没有待确认请求。
        private CanvasType pendingCanvas = CanvasType.Default;

        // 发出导航请求的帧号。请求由 UIManager 在本帧或下一帧消费，确认必须等过这一帧。
        private int pendingFrame;

        /// <summary>红点树根节点。</summary>
        public RedDotBranchNode Root => root;

        /// <summary>上次成功定位的画布，初值为 Default。</summary>
        public CanvasType LastNavigatedCanvas => lastNavigatedCanvas;

        /// <summary>建立红点树根节点，并校验导航按键已经接线。</summary>
        protected override void OnSingletonInitialized()
        {
            root = new RedDotBranchNode();

            Debug.Assert(navigateAction != null, nameof(navigateAction), this);
        }

        /// <summary>启用导航按键，使反引号键能在 Update 中被采样。</summary>
        private void OnEnable()
        {
            navigateAction.action.Enable();
        }

        /// <summary>停用导航按键，与 OnEnable 成对处理。</summary>
        private void OnDisable()
        {
            navigateAction.action.Disable();
        }

        /// <summary>补绑根红点视图；界面重建后由聚合面板再次绑定。</summary>
        private void Start()
        {
            BindRootView(rootView);
        }

        /// <summary>采样导航键并请求定位下一个有待办的画布。</summary>
        /// <remarks>导航键在本组件内采样：UIManager 只接收画布请求，不感知红点。</remarks>
        private void Update()
        {
            if (!navigateAction.action.WasPressedThisFrame()) return;

            TryNavigateToPendingCanvas();
        }

        /// <summary>绑定 OpenMenu 的根红点视图；界面重建后重新绑定即可立即显示。</summary>
        /// <param name="view">OpenMenu 按钮上的红点视图。</param>
        public void BindRootView(RedDotView view)
        {
            rootView = view;
            if (root == null || view == null) return;

            root.BindAndRefresh(view);
        }

        /// <summary>登记画布分支与可用性回调；重复登记以最后一次回调为准。</summary>
        /// <param name="canvas">要登记的画布身份，重复登记不会重复建分支。</param>
        /// <param name="available">待办导航前的可用性判定；返回 false 时跳过该画布。</param>
        public void RegisterCanvas(CanvasType canvas, Func<bool> available)
        {
            if (canvas == CanvasType.Default) return;

            if (EnsureCanvasBranch(canvas) == null) return;
            canvasGates[canvas] = available;
        }

        /// <summary>
        /// 为聚合面板入口绑定画布分支红点视图；没有对应分支时隐藏该红点。
        /// </summary>
        /// <param name="canvas">红点对应的画布身份。</param>
        /// <param name="view">分页按钮上的红点视图。</param>
        public void BindCanvasView(CanvasType canvas, RedDotView view)
        {
            if (canvas == CanvasType.Default || view == null) return;

            canvasViews[canvas] = view;
            if (canvasBranches.TryGetValue(canvas, out RedDotBranchNode branch))
            {
                branch.BindAndRefresh(view);
                return;
            }

            view.SetVisible(false);
        }

        /// <summary>解除画布分支红点绑定；分页复用按钮时先解绑旧画布再绑新画布。</summary>
        /// <param name="canvas">要解除绑定的画布身份。</param>
        public void UnbindCanvasView(CanvasType canvas)
        {
            if (!canvasViews.TryGetValue(canvas, out RedDotView view)) return;

            if (canvasBranches.TryGetValue(canvas, out RedDotBranchNode branch))
            {
                branch.UnbindView(view);
            }

            canvasViews.Remove(canvas);
            if (view != null)
            {
                view.SetVisible(false);
            }
        }

        /// <summary>设置待办导航顺序；重复画布只参与一次，Default 不参与。</summary>
        /// <param name="order">聚合面板展示画布的顺序，通常直接取 canvasToToggle。</param>
        public void SetNavigationOrder(IEnumerable<CanvasType> order)
        {
            navigationOrder.Clear();
            if (order == null) return;

            foreach (CanvasType canvas in order)
            {
                if (canvas == CanvasType.Default || navigationOrder.Contains(canvas)) continue;

                navigationOrder.Add(canvas);
            }
        }

        /// <summary>
        /// 添加业务叶子：校验归属，建立父子关系，订阅一次计数变化并立即刷新父链。
        /// </summary>
        /// <param name="canvas">叶子所属的画布分支。</param>
        /// <param name="leaf">要挂接的业务叶子；已经挂接在树上时拒绝。</param>
        /// <param name="initialCount">按当前业务数据算好的初始计数，挂接时静默写入。</param>
        /// <returns>叶子成功挂接到画布分支时返回 true。</returns>
        public bool AddLeaf(CanvasType canvas, RedDotLeafBase leaf, int initialCount)
        {
            if (leaf == null || canvas == CanvasType.Default) return false;

            // 节点已经挂接在树上时拒绝重复挂接。
            if (leaf.Parent != null) return false;

            RedDotBranchNode branch = EnsureCanvasBranch(canvas);
            if (branch == null) return false;

            leaf.ApplyInitialCount(initialCount);
            leaf.CountChanged += OnLeafCountChanged;
            branch.Add(leaf);
            RecalculateFrom(branch);
            return true;
        }

        /// <summary>移除业务叶子：退订、脱离父子关系、解除表现绑定并刷新原父链。</summary>
        /// <param name="leaf">要摘除的叶子；没有挂接时直接返回。</param>
        public void RemoveLeaf(RedDotLeafBase leaf)
        {
            if (leaf == null) return;

            RedDotBranchNode parent = leaf.Parent;
            if (parent == null) return;

            leaf.CountChanged -= OnLeafCountChanged;
            parent.Remove(leaf);

            RedDotView view = leaf.BoundView;
            if (view != null)
            {
                leaf.UnbindView(view);
                view.SetVisible(false);
            }

            RecalculateFrom(parent);
        }

        /// <summary>
        /// 从上次成功目标之后循环寻找有待办且当前允许打开的画布，一次消费一个目标。
        /// </summary>
        /// <returns>本帧已经请求打开或聚焦一个画布时返回 true。</returns>
        public bool TryNavigateToPendingCanvas()
        {
            if (navigationOrder.Count == 0) return false;

            UIManager uiManager = UIManager.Instance;
            if (uiManager == null) return false;

            int startIndex = IndexOfLastNavigated();
            for (int step = 1; step <= navigationOrder.Count; step++)
            {
                int index = (startIndex + step) % navigationOrder.Count;
                CanvasType canvas = navigationOrder[index];
                if (!HasPending(canvas) || !IsCanvasAvailable(canvas)) continue;

                pendingCanvas = canvas;
                pendingFrame = Time.frameCount;
                uiManager.RequestCanvasToggle(canvas);
                return true;
            }

            return false;
        }

        /// <summary>导航请求由 UIManager 消费，下一帧再确认界面是否真正打开；失败保持游标。</summary>
        private void LateUpdate()
        {
            if (pendingCanvas == CanvasType.Default) return;
            if (Time.frameCount <= pendingFrame) return;

            UICacheManager cache = UICacheManager.Instance;
            if (cache != null && cache.IsCanvasOpen(pendingCanvas))
            {
                lastNavigatedCanvas = pendingCanvas;
            }

            pendingCanvas = CanvasType.Default;
        }

        /// <summary>叶子计数变化后的回调：从它的父分支开始向上重算。</summary>
        private void OnLeafCountChanged(RedDotLeafBase leaf)
        {
            RecalculateFrom(leaf.Parent);
        }

        /// <summary>从给定分支开始向上重算，直到根节点；每个祖先只汇总一次。</summary>
        private void RecalculateFrom(RedDotBranchNode branch)
        {
            while (branch != null)
            {
                branch.Recalculate();
                branch = branch.Parent;
            }
        }

        /// <summary>取画布分支，没有则建立并挂到根节点；已有视图会补绑到新分支。</summary>
        private RedDotBranchNode EnsureCanvasBranch(CanvasType canvas)
        {
            if (root == null) return null;

            if (canvasBranches.TryGetValue(canvas, out RedDotBranchNode existing)) return existing;

            RedDotBranchNode branch = new RedDotBranchNode();
            canvasBranches.Add(canvas, branch);
            root.Add(branch);

            if (canvasViews.TryGetValue(canvas, out RedDotView view))
            {
                branch.BindAndRefresh(view);
            }

            return branch;
        }

        /// <summary>判断画布当前是否还有待办，用于导航筛选。</summary>
        private bool HasPending(CanvasType canvas)
        {
            return canvasBranches.TryGetValue(canvas, out RedDotBranchNode branch) && branch.Count > 0;
        }

        /// <summary>判断画布当前是否允许打开</summary>
        private bool IsCanvasAvailable(CanvasType canvas)
        {
            return !canvasGates.TryGetValue(canvas, out Func<bool> gate) || gate == null || gate();
        }

        /// <summary>取上次定位画布在导航顺序中的下标；返回 -1 表示从第一项开始找。</summary>
        private int IndexOfLastNavigated()
        {
            for (int i = 0; i < navigationOrder.Count; i++)
            {
                if (navigationOrder[i] == lastNavigatedCanvas) return i;
            }

            return -1;
        }
    }
}
