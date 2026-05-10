namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal readonly struct WpfGridRegionRenderDirectives
    {
        internal WpfGridRegionRenderDirectives(bool forceCompactSize, bool autoSizeToContent = false)
        {
            ForceCompactSize = forceCompactSize;
            AutoSizeToContent = autoSizeToContent;
        }

        internal bool ForceCompactSize { get; }

        internal bool AutoSizeToContent { get; }
    }
}
