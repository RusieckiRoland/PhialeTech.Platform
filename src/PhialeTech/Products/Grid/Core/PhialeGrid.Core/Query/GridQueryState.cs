using System;
using System.Collections.Generic;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GridQueryState
    {
        public GridQueryState(
            IReadOnlyList<GridSortDescriptor> sorts,
            GridFilterGroup filterGroup,
            IReadOnlyList<GridGroupDescriptor> groups,
            IReadOnlyList<GridSummaryDescriptor> summaries)
        {
            Sorts = sorts ?? Array.Empty<GridSortDescriptor>();
            FilterGroup = filterGroup ?? GridFilterGroup.EmptyAnd();
            Groups = groups ?? Array.Empty<GridGroupDescriptor>();
            Summaries = summaries ?? Array.Empty<GridSummaryDescriptor>();
        }

        public IReadOnlyList<GridSortDescriptor> Sorts { get; }

        public GridFilterGroup FilterGroup { get; }

        public IReadOnlyList<GridGroupDescriptor> Groups { get; }

        public IReadOnlyList<GridSummaryDescriptor> Summaries { get; }

        public GridQueryState WithSorts(IReadOnlyList<GridSortDescriptor> sorts)
        {
            return new GridQueryState(sorts, FilterGroup, Groups, Summaries);
        }

        public GridQueryState WithFilterGroup(GridFilterGroup filterGroup)
        {
            return new GridQueryState(Sorts, filterGroup, Groups, Summaries);
        }

        public GridQueryState WithGroups(IReadOnlyList<GridGroupDescriptor> groups)
        {
            return new GridQueryState(Sorts, FilterGroup, groups, Summaries);
        }

        public GridQueryState WithSummaries(IReadOnlyList<GridSummaryDescriptor> summaries)
        {
            return new GridQueryState(Sorts, FilterGroup, Groups, summaries);
        }
    }
}
