using System;
using System.Collections.Generic;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.State
{
    public sealed class GridStateSnapshot
    {
        public GridStateSnapshot(
            GridLayoutSnapshot layout,
            IReadOnlyList<GridSortDescriptor> sorts,
            GridFilterGroup filters,
            IReadOnlyList<GridGroupDescriptor> groups,
            IReadOnlyList<GridSummaryDescriptor> summaries,
            GridRegionLayoutSnapshot regionLayout = null,
            string globalSearchText = null,
            bool? selectCurrentRow = null,
            bool? multiSelect = null,
            bool? showRowNumbers = null,
            GridRowNumberingMode? rowNumberingMode = null)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Sorts = sorts ?? Array.Empty<GridSortDescriptor>();
            Filters = filters ?? GridFilterGroup.EmptyAnd();
            Groups = groups ?? Array.Empty<GridGroupDescriptor>();
            Summaries = summaries ?? Array.Empty<GridSummaryDescriptor>();
            RegionLayout = regionLayout ?? new GridRegionLayoutSnapshot(Array.Empty<GridRegionLayoutState>());
            GlobalSearchText = globalSearchText ?? string.Empty;
            SelectCurrentRow = selectCurrentRow;
            MultiSelect = multiSelect;
            ShowRowNumbers = showRowNumbers;
            RowNumberingMode = rowNumberingMode;
        }

        public GridLayoutSnapshot Layout { get; }

        public IReadOnlyList<GridSortDescriptor> Sorts { get; }

        public GridFilterGroup Filters { get; }

        public IReadOnlyList<GridGroupDescriptor> Groups { get; }

        public IReadOnlyList<GridSummaryDescriptor> Summaries { get; }

        public GridRegionLayoutSnapshot RegionLayout { get; }

        public string GlobalSearchText { get; }

        public bool? SelectCurrentRow { get; }

        public bool? MultiSelect { get; }

        public bool? ShowRowNumbers { get; }

        public GridRowNumberingMode? RowNumberingMode { get; }
    }
}
