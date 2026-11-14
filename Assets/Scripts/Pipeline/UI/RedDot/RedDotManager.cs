using System;
using System.Collections.Generic;
using Contracts.Base;
using Contracts.Enums;
using Pipeline.UI.Runtime;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [Tooltip("待办导航按键（键盘反引号，即 ~ 键）。按下后按画布顺序打开或聚焦有待办的界面。")]
        [SerializeField]
        private InputActionReference navigateAction;

        // 画布身份到分支的映射；分支在所属域登记时建立。
        private readonly Dictionary<CanvasType, RedDotBranchNode> canvasBranches = new();

        // 所属域登记的画布可用性回调；未登记表示可以直接打开。
        private readonly Dictionary<CanvasType, Func<bool>> canvasGates = new();

        // 当前绑定的画布红点表现，分支建立后自动补绑。
        private readonly Dictionary<CanvasType, Image> canvasImages = new();

        // 待办导航顺序，取值来源是聚合面板已配置的画布列表。
        private readonly List<CanvasType> navigationOrder = new();

        private RedDotBranchNode root;
        private Image rootImage;
        private CanvasType lastNavigatedCanvas = CanvasType.Default;
        private CanvasType pendingCanvas = CanvasType.Default;
        private int pendingFrame;

        /// <summary>红点树根节点。</summary>
        public RedDotBranchNode Root => root;

        /// <summary>上次成功定位的画布，初值为 Default。</summary>
        public CanvasType LastNavigatedCanvas => lastNavigatedCanvas;

        protected override void OnSingletonInitialized()
        {
            root = new RedDotBranchNode();

            Debug.Assert(navigateAction != null, nameof(navigateAction), this);
        }

        private void OnEnable()
        {
            navigateAction.action.Enable();
        }

        private void OnDisable()
        {
            navigateAction.action.Disable();
        }

        private void Start()
        {
            BindRootImage(rootImage);
        }

        private void Update()
        {
            if (!navigateAction.action.WasPressedThisFrame()) return;

            TryNavigateToPendingCanvas();
        }

        /// <summary>绑定 OpenMenu 的根红点表现；界面重建后重新绑定即可立即显示。</summary>
        public void BindRootImage(Image image)
        {
            rootImage = image;
            if (root == null || image == null) return;

            root.BindImage(image);
        }

        /// <summary>登记画布分支与可用性回调；重复登记以最后一次回调为准。</summary>
        public void RegisterCanvas(CanvasType canvas, Func<bool> available)
        {
            if (canvas == CanvasType.Default) return;

            if (EnsureCanvasBranch(canvas) == null) return;
            canvasGates[canvas] = available;
        }

        /// <summary>
        /// 为聚合面板入口绑定画布分支红点；没有对应分支时隐藏该红点。
        /// </summary>
        public void BindCanvasImage(CanvasType canvas, Image image)
        {
            if (canvas == CanvasType.Default || image == null) return;

            canvasImages[canvas] = image;
            if (canvasBranches.TryGetValue(canvas, out RedDotBranchNode branch))
            {
                branch.BindImage(image);
                return;
            }

            image.enabled = false;
        }

        /// <summary>解除画布分支红点绑定；分页复用按钮时先解绑旧画布再绑新画布。</summary>
        public void UnbindCanvasImage(CanvasType canvas)
        {
            if (!canvasImages.TryGetValue(canvas, out Image image)) return;

            if (canvasBranches.TryGetValue(canvas, out RedDotBranchNode branch))
            {
                branch.UnbindImage(image);
            }

            canvasImages.Remove(canvas);
            if (image != null)
            {
                image.enabled = false;
            }
        }

        /// <summary>设置待办导航顺序；重复画布只参与一次，Default 不参与。</summary>
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
            branch.Attach(leaf);
            RecalculateFrom(branch);
            return true;
        }

        /// <summary>移除业务叶子：退订、脱离父子关系、解除表现绑定并刷新原父链。</summary>
        public void RemoveLeaf(RedDotLeafBase leaf)
        {
            if (leaf == null) return;

            RedDotBranchNode parent = leaf.Parent;
            if (parent == null) return;

            leaf.CountChanged -= OnLeafCountChanged;
            parent.Detach(leaf);

            Image image = leaf.BoundImage;
            if (image != null)
            {
                leaf.UnbindImage(image);
                image.enabled = false;
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

        // 导航请求在本帧由 UIManager 消费，下一帧再确认界面是否真正打开，失败保持游标。
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

        private void OnLeafCountChanged(RedDotLeafBase leaf)
        {
            RecalculateFrom(leaf.Parent);
        }

        private void RecalculateFrom(RedDotBranchNode branch)
        {
            while (branch != null)
            {
                branch.Recalculate();
                branch = branch.Parent;
            }
        }

        private RedDotBranchNode EnsureCanvasBranch(CanvasType canvas)
        {
            if (root == null) return null;

            if (canvasBranches.TryGetValue(canvas, out RedDotBranchNode existing)) return existing;

            RedDotBranchNode branch = new RedDotBranchNode();
            canvasBranches.Add(canvas, branch);
            root.Attach(branch);

            if (canvasImages.TryGetValue(canvas, out Image image))
            {
                branch.BindImage(image);
            }

            return branch;
        }

        private bool HasPending(CanvasType canvas)
        {
            return canvasBranches.TryGetValue(canvas, out RedDotBranchNode branch) && branch.Count > 0;
        }

        private bool IsCanvasAvailable(CanvasType canvas)
        {
            return !canvasGates.TryGetValue(canvas, out Func<bool> gate) || gate == null || gate();
        }

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
