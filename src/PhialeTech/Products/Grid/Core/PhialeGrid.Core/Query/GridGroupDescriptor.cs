using System;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupDescriptor
    {
        public GridGroupDescriptor(string columnId, GridSortDirection direction = GridSortDirection.Ascending)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            ColumnId = columnId;
            Direction = direction;
        }

        public string ColumnId { get; }

        public GridSortDirection Direction { get; }
    }
}
