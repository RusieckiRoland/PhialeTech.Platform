using System;
using System.Collections.Generic;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GridQueryResult<T>
    {
        public GridQueryResult(IReadOnlyList<T> items, int totalCount, IReadOnlyList<GridGroupNode<T>> groupedItems, GridSummarySet summary)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            GroupedItems = groupedItems ?? Array.Empty<GridGroupNode<T>>();
            Summary = summary ?? GridSummarySet.Empty;
            TotalCount = totalCount;
        }

        public IReadOnlyList<T> Items { get; }

        public int TotalCount { get; }

        public IReadOnlyList<GridGroupNode<T>> GroupedItems { get; }

        public GridSummarySet Summary { get; }
    }
}
