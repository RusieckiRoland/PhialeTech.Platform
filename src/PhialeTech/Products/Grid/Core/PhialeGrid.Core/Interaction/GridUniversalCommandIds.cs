namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Stable command identifiers shared between platform hosts and Core.
    /// </summary>
    public static class GridUniversalCommandIds
    {
        public const string BeginEdit = "grid.beginEdit";
        public const string PostEdit = "grid.postEdit";
        public const string CancelEdit = "grid.cancelEdit";
        public const string ToggleTopCommandRegion = "grid.region.toggle.topCommand";
        public const string ToggleGroupingRegion = "grid.region.toggle.grouping";
        public const string ToggleSummaryBottomRegion = "grid.region.toggle.summaryBottom";
        public const string ToggleSideToolRegion = "grid.region.toggle.sideTools";
    }
}
