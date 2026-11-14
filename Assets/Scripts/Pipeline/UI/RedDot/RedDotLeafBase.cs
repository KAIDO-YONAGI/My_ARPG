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
        public event Action<RedDotLeafBase> CountChanged;

        /// <summary>写入叶子计数并通知父链重算；计数非负，同值直接返回。</summary>
        protected void SetCount(int count)
        {
            if (!ApplyCount(count)) return;

            CountChanged?.Invoke(this);
        }

        /// <summary>挂接时由管理器写入初始计数，不触发变化通知。</summary>
        internal void ApplyInitialCount(int count)
        {
            ApplyCount(count);
        }
    }
}
