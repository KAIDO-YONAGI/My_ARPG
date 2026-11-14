using Pipeline.UI.RedDot;

namespace Gameplay.Player
{
    /// <summary>
    /// 属性画布的红点叶子。表示伤害或速度存在玩家尚未查看的提升。
    /// </summary>
    /// <remarks>
    /// 计数只在 0 与 1 之间切换，连续提升保持同一个未查看标记。
    /// 提升与查看的判定由 <see cref="StatsRedDotTracker"/> 完成，叶子只保存状态。
    /// </remarks>
    public sealed class StatsRedDotLeaf : RedDotLeafBase
    {
        /// <summary>标记存在未查看的属性提升；已经标记时计数保持 1。</summary>
        public void MarkUnread() => SetCount(1);

        /// <summary>清除未查看标记；已经清除时计数保持 0。</summary>
        public void ClearUnread() => SetCount(0);
    }
}
