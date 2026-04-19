using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class GridCellChange
    {
        public GridCellChange(string columnId, object originalValue, object currentValue)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            ColumnId = columnId;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }

        public string ColumnId { get; }

        public object OriginalValue { get; }

        public object CurrentValue { get; }
    }
}
