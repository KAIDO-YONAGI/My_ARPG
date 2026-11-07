namespace Contracts.State
{
    /// <summary>保存运行期值及其版本；读取者各自记录已绘制的版本。</summary>
    public struct VersionedValue<T>
    {
        private T value;
        private ulong version;

        /// <summary>创建带初值的状态；默认构造的零版本也可由读取者正常识别。</summary>
        public VersionedValue(T initial)
        {
            value = initial;
            version = 1;
        }

        /// <summary>当前值；写入时推进版本，包括相同引用下的内容更新。</summary>
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                Touch();
            }
        }

        /// <summary>当前数据版本；由各读取者独立比较。</summary>
        public ulong Version => version;

        /// <summary>写入新值并推进版本。</summary>
        public void Set(T next)
        {
            value = next;
            Touch();
        }

        /// <summary>集合内容原地修改后推进版本；不复制集合或创建快照。</summary>
        public void Touch()
        {
            unchecked { version++; }
        }
    }
}
