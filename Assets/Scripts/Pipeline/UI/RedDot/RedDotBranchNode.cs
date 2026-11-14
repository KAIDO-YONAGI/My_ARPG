using System.Collections.Generic;

namespace Pipeline.UI.RedDot
{
    /// <summary>
    /// 红点树分支节点。汇总直接子节点的计数并刷新自身表现，根节点与画布分支共用本类型。
    /// </summary>
    /// <remarks>
    /// 子节点集合由 <see cref="RedDotManager"/> 的结构操作维护，对外只读；
    /// 分支计数始终等于直接子节点计数之和。
    /// </remarks>
    public sealed class RedDotBranchNode : RedDotNodeBase
    {
        // 直接子节点，挂接顺序即添加顺序；计数汇总按这份列表逐个累加。
        private readonly List<RedDotNodeBase> children = new();

        /// <summary>直接子节点，对外只读。</summary>
        public IReadOnlyList<RedDotNodeBase> Children => children;

        /// <summary>重新汇总直接子节点计数并刷新表现。</summary>
        internal void Recalculate()
        {
            int total = 0;
            for (int i = 0; i < children.Count; i++)
            {
                total += children[i].Count;
            }

            ApplyCount(total);
        }

        /// <summary>挂接一个子节点；归属校验由调用方完成。</summary>
        /// <param name="child">要挂到本分支下的节点；重复挂接与树归属由管理器校验。</param>
        internal void Attach(RedDotNodeBase child)
        {
            children.Add(child);
            child.Parent = this;
        }

        /// <summary>解除一个子节点的挂接并清空其父引用。</summary>
        /// <param name="child">期望摘除的节点。</param>
        /// <returns>子节点确实挂在本分支下时返回 true。</returns>
        internal bool Detach(RedDotNodeBase child)
        {
            if (!children.Remove(child)) return false;

            child.Parent = null;
            return true;
        }
    }
}
