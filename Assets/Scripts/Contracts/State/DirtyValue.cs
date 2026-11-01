namespace Contracts.State
{
    /// <summary>保存一个运行期值，并记录它是否需要同步到界面。</summary>
    public struct DirtyValue<T>
    {
        private T value;
        private bool mark;

        /// <summary>创建一个带初值的值；<paramref name="dirty"/> 为 true 时立即标记为待刷新。</summary>
        public DirtyValue(T initial, bool dirty = false)
        {
            value = initial;
            mark = dirty;
        }

        /// <summary>当前值；写入时自动置脏。</summary>
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                mark = true;
            }
        }

        /// <summary>是否已有变化尚未被视图读取。</summary>
        public bool IsDirty => mark;

        /// <summary>写入新值并置脏。</summary>
        public void Set(T next)
        {
            value = next;
            mark = true;
        }

        /// <summary>写入新值并清除标记，用于初始化或强制同步。</summary>
        public void SetClean(T next)
        {
            value = next;
            mark = false;
        }

        /// <summary>值未变但需要视图重绘时，单独置脏。</summary>
        public void MarkDirty()
        {
            mark = true;
        }

        /// <summary>视图完成刷新后清除脏标记。</summary>
        public void ClearMark()
        {
            mark = false;
        }
    }
}
