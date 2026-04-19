using System;
using System.Collections.Generic;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupedQueryRequest
    {
        public GridGroupedQueryRequest(
            int offset,
            int size,
            IReadOnlyList<GridSortDescriptor> sorts,
            GridFilterGroup filterGroup,
            IReadOnlyList<GridGroupDescriptor> groups,
            IReadOnlyList<GridSummaryDescriptor> summaries,
            GridGroupExpansionState expansionState)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            Offset = offset;
            Size = size;
            Sorts = sorts ?? Array.Empty<GridSortDescriptor>();
            FilterGroup = filterGroup ?? GridFilterGroup.EmptyAnd();
            Groups = groups ?? Array.Empty<GridGroupDescriptor>();
            Summaries = summaries ?? Array.Empty<GridSummaryDescriptor>();
            ExpansionState = expansionState ?? new GridGroupExpansionState();
        }

        public int Offset { get; }

        public int Size { get; }

        public IReadOnlyList<GridSortDescriptor> Sorts { get; }

        public GridFilterGroup FilterGroup { get; }

        public IReadOnlyList<GridGroupDescriptor> Groups { get; }

        public IReadOnlyList<GridSummaryDescriptor> Summaries { get; }

        public GridGroupExpansionState ExpansionState { get; }
    }
}
