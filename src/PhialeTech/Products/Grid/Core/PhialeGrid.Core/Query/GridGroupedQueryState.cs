using System;
using System.Collections.Generic;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupedQueryState
    {
        public GridGroupedQueryState(
            IReadOnlyList<GridSortDescriptor> sorts,
            GridFilterGroup filterGroup,
            IReadOnlyList<GridGroupDescriptor> groups,
            IReadOnlyList<GridSummaryDescriptor> summaries,
            GridGroupExpansionState expansionState)
        {
            Sorts = sorts ?? Array.Empty<GridSortDescriptor>();
            FilterGroup = filterGroup ?? GridFilterGroup.EmptyAnd();
            Groups = groups ?? Array.Empty<GridGroupDescriptor>();
            Summaries = summaries ?? Array.Empty<GridSummaryDescriptor>();
            ExpansionState = expansionState ?? new GridGroupExpansionState();
        }

        public IReadOnlyList<GridSortDescriptor> Sorts { get; }

        public GridFilterGroup FilterGroup { get; }

        public IReadOnlyList<GridGroupDescriptor> Groups { get; }

        public IReadOnlyList<GridSummaryDescriptor> Summaries { get; }

        public GridGroupExpansionState ExpansionState { get; }

        public GridGroupedQueryState WithSorts(IReadOnlyList<GridSortDescriptor> sorts)
        {
            return new GridGroupedQueryState(sorts, FilterGroup, Groups, Summaries, ExpansionState);
        }

        public GridGroupedQueryState WithFilterGroup(GridFilterGroup filterGroup)
        {
            return new GridGroupedQueryState(Sorts, filterGroup, Groups, Summaries, ExpansionState);
        }

        public GridGroupedQueryState WithGroups(IReadOnlyList<GridGroupDescriptor> groups)
        {
            return new GridGroupedQueryState(Sorts, FilterGroup, groups, Summaries, ExpansionState);
        }

        public GridGroupedQueryState WithSummaries(IReadOnlyList<GridSummaryDescriptor> summaries)
        {
            return new GridGroupedQueryState(Sorts, FilterGroup, Groups, summaries, ExpansionState);
        }

        public GridGroupedQueryState WithExpansionState(GridGroupExpansionState expansionState)
        {
            return new GridGroupedQueryState(Sorts, FilterGroup, Groups, Summaries, expansionState);
        }
    }
}
