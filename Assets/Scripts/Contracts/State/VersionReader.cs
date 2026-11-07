namespace Contracts.State
{
    /// <summary>单个读取者的绘制记录；首次读取、数据源切换和版本变化均需要刷新。</summary>
    public struct VersionReader
    {
        private object source;
        private ulong version;
        private bool hasRead;

        public bool NeedsRefresh(object currentSource, ulong currentVersion) =>
            !hasRead || !ReferenceEquals(source, currentSource) || version != currentVersion;

        /// <summary>绘制完成后记录绘制前取得的版本，保留绘制期间产生的新变化。</summary>
        public void Record(object currentSource, ulong renderedVersion)
        {
            source = currentSource;
            version = renderedVersion;
            hasRead = true;
        }

        /// <summary>界面打开或控件重建时强制本读取者刷新，不修改数据源。</summary>
        public void Invalidate() => hasRead = false;
    }
}
