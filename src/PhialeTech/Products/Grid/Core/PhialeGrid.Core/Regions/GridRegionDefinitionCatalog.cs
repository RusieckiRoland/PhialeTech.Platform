namespace PhialeGrid.Core.Regions
{
    public static class GridRegionDefinitionCatalog
    {
        public static GridRegionDefinition[] CreateDefault()
        {
            return new[]
            {
                new GridRegionDefinition(
                    GridRegionKind.CoreGridSurface,
                    GridRegionHostKind.Surface,
                    GridRegionPlacement.Center,
                    GridRegionContentKind.GridSurface,
                    GridRegionState.Open,
                    defaultSize: null,
                    minSize: null,
                    maxSize: null,
                    canCollapse: false,
                    canClose: false,
                    canResize: false,
                    canActivate: false),
                new GridRegionDefinition(
                    GridRegionKind.TopCommandRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Top,
                    GridRegionContentKind.CommandBar,
                    GridRegionState.Open,
                    defaultSize: 44d,
                    minSize: 36d,
                    maxSize: 44d,
                    canCollapse: true,
                    canClose: true,
                    canResize: false,
                    canActivate: false),
                new GridRegionDefinition(
                    GridRegionKind.GroupingRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Top,
                    GridRegionContentKind.GroupingDropZone,
                    GridRegionState.Open,
                    defaultSize: 56d,
                    minSize: 56d,
                    maxSize: 220d,
                    canCollapse: true,
                    canClose: true,
                    canResize: true,
                    canActivate: false),
                new GridRegionDefinition(
                    GridRegionKind.SummaryBottomRegion,
                    GridRegionHostKind.Strip,
                    GridRegionPlacement.Bottom,
                    GridRegionContentKind.Summary,
                    GridRegionState.Open,
                    defaultSize: 56d,
                    minSize: 56d,
                    maxSize: 180d,
                    canCollapse: false,
                    canClose: true,
                    canResize: false,
                    canActivate: false),
                new GridRegionDefinition(
                    GridRegionKind.SideToolRegion,
                    GridRegionHostKind.Pane,
                    GridRegionPlacement.Right,
                    GridRegionContentKind.ToolPane,
                    GridRegionState.Closed,
                    defaultSize: 320d,
                    minSize: 220d,
                    maxSize: 520d,
                    canCollapse: true,
                    canClose: true,
                    canResize: true,
                    canActivate: true),
            };
        }
    }
}
