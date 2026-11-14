using UnityEngine;
using UnityEngine.UI;

namespace Pipeline.UI.RedDot
{
    /// <summary>
    /// 红点预制体的表现组件。红点统一由 Assets/Prefabs/UI/RedDot.prefab 提供，
    /// 节点绑定本组件并按计数切换显隐，不直接操作图片。
    /// </summary>
    /// <remarks>
    /// 视图只负责显隐，不保存计数；宿主对象的启停仍由所属界面的生命周期控制。
    /// </remarks>
    public sealed class RedDotView : MonoBehaviour
    {
        // 预制体根节点上的红点图片，由预制体序列化接线。
        [SerializeField] private Image image;

        /// <summary>按节点计数切换红点显隐。</summary>
        /// <param name="visible">true 显示红点，false 隐藏。</param>
        public void SetVisible(bool visible)
        {
            image.enabled = visible;
        }

        /// <summary>校验预制体上的图片接线，缺引用由断言显式报出。</summary>
        private void Awake()
        {
            Debug.Assert(image != null, nameof(image), this);
        }
    }
}
