using System;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupingDragPayload
    {
        public GridGroupingDragPayload(string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            ColumnId = columnId;
        }

        public string ColumnId { get; }
    }
}
