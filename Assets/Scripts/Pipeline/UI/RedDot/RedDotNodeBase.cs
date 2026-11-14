using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pipeline.UI.RedDot
{
    /// <summary>
    /// 红点树节点的共同基类。持有计数、父分支与当前绑定的表现图片，
    /// 负责计数写入与表现刷新；树结构关系由 <see cref="RedDotManager"/> 维护。
    /// </summary>
    /// <remarks>
    /// 计数只通过受保护入口写入，派生叶子与分支各自决定计数来源。
    /// 表现按 <see cref="Image.enabled"/> 开关，宿主对象的启停仍由原 UI 生命周期控制。
    /// </remarks>
    public abstract class RedDotNodeBase
    {
        // 当前绑定的表现图片。界面打开时绑定、关闭时解绑；为 null 只表示没有表现载体，计数继续保存。
        private Image image;

        /// <summary>当前计数，对外只读。</summary>
        public int Count { get; private set; }

        /// <summary>所属父分支，对外只读；根节点为 null。</summary>
        public RedDotBranchNode Parent { get; internal set; }

        /// <summary>当前绑定的表现图片；未绑定时为 null。</summary>
        public Image BoundImage => image;

        /// <summary>绑定表现图片并立即按当前计数刷新；原表现引用被替换。</summary>
        /// <param name="target">要绑定的表现图片；与当前引用相同时不做事。</param>
        public void BindImage(Image target)
        {
            if (image == target) return;

            image = target;
            RefreshImage();
        }

        /// <summary>只在参数与当前引用一致时解除绑定，防止旧界面回调清除新绑定。</summary>
        /// <param name="target">期望解除的引用；与当前绑定不一致时忽略。</param>
        public void UnbindImage(Image target)
        {
            if (image != target) return;

            image = null;
        }

        /// <summary>按当前计数刷新表现；未绑定图片时继续保存计数。</summary>
        public void RefreshImage()
        {
            if (image == null) return;

            image.enabled = Count > 0;
        }

        /// <summary>
        /// 写入计数：拒绝负数，同值静默返回，变化后刷新自身表现。
        /// </summary>
        /// <param name="count">新的计数，不能为负。</param>
        /// <returns>计数实际发生变化时返回 true。</returns>
        protected bool ApplyCount(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "红点计数不能为负。");
            }

            if (count == Count) return false;

            Count = count;
            RefreshImage();
            return true;
        }
    }
}
