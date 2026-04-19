using System;
using System.Collections.Generic;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupedQueryResult<T>
    {
        public GridGroupedQueryResult(
            IReadOnlyList<GridGroupFlatRow<T>> rows,
            int visibleRowCount,
            int totalItemCount,
            int topLevelGroupCount,
            IReadOnlyList<string> groupIds,
            GridSummarySet summary)
        {
            if (visibleRowCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleRowCount));
            }

            if (totalItemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalItemCount));
            }

            if (topLevelGroupCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topLevelGroupCount));
            }

            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            VisibleRowCount = visibleRowCount;
            TotalItemCount = totalItemCount;
            TopLevelGroupCount = topLevelGroupCount;
            GroupIds = groupIds ?? Array.Empty<string>();
            Summary = summary ?? GridSummarySet.Empty;
        }

        public IReadOnlyList<GridGroupFlatRow<T>> Rows { get; }

        public int VisibleRowCount { get; }

        public int TotalItemCount { get; }

        public int TopLevelGroupCount { get; }

        public IReadOnlyList<string> GroupIds { get; }

        public GridSummarySet Summary { get; }
    }
}
