namespace Contracts
{
    /// <summary>
    /// 带脏标记的值：数值与 Mark 标记放在同一个结构体里。
    /// 管理器写入时自动置脏，视图读取后清除标记。
    /// </summary>
    /// <typeparam name="T">值类型或引用类型。</typeparam>
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

        /// <summary>视图读完当前值后清除标记。</summary>
        public void ClearMark()
        {
            mark = false;
        }
    }
}
