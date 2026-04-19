namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal readonly struct WpfGridRegionRenderDirectives
    {
        internal WpfGridRegionRenderDirectives(bool forceCompactSize, bool allowResize)
        {
            ForceCompactSize = forceCompactSize;
            AllowResize = allowResize;
        }

        internal bool ForceCompactSize { get; }

        internal bool AllowResize { get; }
    }
}
