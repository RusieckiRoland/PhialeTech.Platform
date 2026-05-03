namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal readonly struct WpfGridRegionRenderDirectives
    {
        internal WpfGridRegionRenderDirectives(bool forceCompactSize)
        {
            ForceCompactSize = forceCompactSize;
        }

        internal bool ForceCompactSize { get; }
    }
}
