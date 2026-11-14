using System;

namespace Pipeline.UI.RedDot
{
    /// <summary>
    /// 红点树叶子的共同基类。业务条件变化时通过 <see cref="SetCount"/> 写入计数，
    /// 计数变化后发出事件，由管理器沿父链逐级重算。
    /// </summary>
    /// <remarks>
    /// 业务叶子读取各自所属管理器的规则与数据，通用节点不认识具体玩法。
    /// 一个叶子同时只属于一个父分支，挂接与摘除由 <see cref="RedDotManager"/> 完成。
    /// </remarks>
    public abstract class RedDotLeafBase : RedDotNodeBase
    {
        /// <summary>叶子计数变化时触发，携带自身引用。</summary>
        /// <remarks>订阅者只有 <see cref="RedDotManager"/>，它在挂接时订阅、摘除时退订。</remarks>
        public event Action<RedDotLeafBase> CountChanged;

        /// <summary>写入叶子计数并通知父链重算；计数非负，同值直接返回。</summary>
        /// <param name="count">新的计数，业务叶子取 0 或 1。</param>
        protected void SetCount(int count)
        {
            if (!ApplyCount(count)) return;

            CountChanged?.Invoke(this);
        }

        /// <summary>挂接时由管理器写入初始计数，不触发变化通知。</summary>
        /// <param name="count">挂接前按当前业务数据算好的初始计数。</param>
        internal void ApplyInitialCount(int count)
        {
            ApplyCount(count);
        }
    }
}
