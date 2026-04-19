using System.Collections.Generic;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Rendering;

namespace PhialeGrid.Core.State
{
    public sealed class GridViewState
    {
        public GridViewState()
        {
            Version = 4;
            Columns = new List<GridViewColumnState>();
            Sorts = new List<GridViewSortState>();
            Filters = new GridViewFilterGroupState();
            Groups = new List<GridViewGroupState>();
            Summaries = new List<GridViewSummaryState>();
            RegionLayout = new List<GridViewRegionState>();
            GlobalSearchText = string.Empty;
        }

        public int Version { get; set; }

        public List<GridViewColumnState> Columns { get; set; }

        public List<GridViewSortState> Sorts { get; set; }

        public GridViewFilterGroupState Filters { get; set; }

        public List<GridViewGroupState> Groups { get; set; }

        public List<GridViewSummaryState> Summaries { get; set; }

        public List<GridViewRegionState> RegionLayout { get; set; }

        public string GlobalSearchText { get; set; }

        public bool? SelectCurrentRow { get; set; }

        public bool? MultiSelect { get; set; }

        public bool? ShowRowNumbers { get; set; }

        public GridRowNumberingMode? RowNumberingMode { get; set; }
    }

    public sealed class GridViewColumnState
    {
        public string ColumnId { get; set; }

        public int DisplayIndex { get; set; }

        public double Width { get; set; }

        public bool IsVisible { get; set; }

        public bool IsFrozen { get; set; }
    }

    public sealed class GridViewSortState
    {
        public string ColumnId { get; set; }

        public GridSortDirection Direction { get; set; }
    }

    public sealed class GridViewFilterGroupState
    {
        public GridViewFilterGroupState()
        {
            LogicalOperator = GridLogicalOperator.And;
            Filters = new List<GridViewFilterState>();
        }

        public GridLogicalOperator LogicalOperator { get; set; }

        public List<GridViewFilterState> Filters { get; set; }
    }

    public sealed class GridViewFilterState
    {
        public string ColumnId { get; set; }

        public GridFilterOperator Operator { get; set; }

        public bool HasValue { get; set; }

        public string ValueText { get; set; }

        public bool HasSecondValue { get; set; }

        public string SecondValueText { get; set; }
    }

    public sealed class GridViewGroupState
    {
        public string ColumnId { get; set; }

        public GridSortDirection Direction { get; set; }
    }

    public sealed class GridViewSummaryState
    {
        public string ColumnId { get; set; }

        public Summaries.GridSummaryType Type { get; set; }
    }

    public sealed class GridViewRegionState
    {
        public GridRegionKind RegionKind { get; set; }

        public GridRegionState State { get; set; }

        public double? Size { get; set; }

        public bool IsActive { get; set; }
    }
}
