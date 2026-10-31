using Pipeline.SO.Events;
using UnityEngine;

namespace Contracts
{
    /// <summary>
    /// 缓存画布的通用实现：界面身份、开关事件、Canvas 与 CanvasGroup 全部由预制体序列化接线，
    /// 运行时不再用 <c>GetComponent</c> 或层级查找补引用。
    /// </summary>
    /// <remarks>
    /// 各业务画布（任务、商店、技能、对话、背包）只需继承本类，
    /// 然后在自己的预制体里把根节点的 Canvas 和 CanvasGroup 拖到对应槽位。
    /// </remarks>
    public class CachedCanvasView : CachedUIView
    {
        [Header("画布身份")]
        [Tooltip("该画布在 CanvasToToggle 中的身份，必须与开关事件上的枚举一致。")]
        [SerializeField] private CanvasToToggle canvasId;

        [Tooltip("显隐请求使用的开关事件；缓存启动时按它订阅。")]
        [SerializeField] private ToggleCanvasEventSO toggleEvent;

        [Header("显示控制")]
        [Tooltip("排序用 Canvas，通常就是预制体根节点上的 Canvas。")]
        [SerializeField] private Canvas viewCanvas;

        [Tooltip("控制显隐、交互和射线阻挡的 CanvasGroup，通常就在预制体根节点。")]
        [SerializeField] private CanvasGroup viewGroup;

        [Tooltip("是否响应开关事件；由外部代码直接控制的画布可关闭。")]
        [SerializeField] private bool listenForToggleEvent = true;

        [Tooltip("按 ESC 时是否允许关闭本画布。")]
        [SerializeField] private bool closeOnEscape = true;

        [Tooltip("打开期间是否阻断底层界面的全局输入。")]
        [SerializeField] private bool blocksGlobalInput;

        public override ToggleCanvasEventSO ToggleEvent => toggleEvent;
        public override CanvasToToggle CanvasId => canvasId;
        public override bool ListenForToggleEvent => listenForToggleEvent;
        public override bool CloseOnEscape => closeOnEscape;
        public override bool BlocksGlobalInput => blocksGlobalInput;
        protected override Canvas ViewCanvas => viewCanvas;
        protected override CanvasGroup ViewGroup => viewGroup;
    }
}
