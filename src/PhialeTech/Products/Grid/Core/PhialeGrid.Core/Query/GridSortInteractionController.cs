using System;
using System.Collections.Generic;
using System.Linq;
using UniversalInput.Contracts;

namespace PhialeGrid.Core.Query
{
    public sealed class GridSortInteractionController
    {
        public IReadOnlyList<GridSortDescriptor> ToggleSort(
            IReadOnlyList<GridSortDescriptor> current,
            string columnId,
            UniversalModifierKeys modifiers)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            if (current == null)
            {
                current = Array.Empty<GridSortDescriptor>();
            }

            var existing = current.FirstOrDefault(sort => string.Equals(sort.ColumnId, columnId, StringComparison.OrdinalIgnoreCase));
            var nextDirection = existing == null || existing.Direction == GridSortDirection.Descending
                ? GridSortDirection.Ascending
                : GridSortDirection.Descending;

            var keepExisting = (modifiers & UniversalModifierKeys.Shift) == UniversalModifierKeys.Shift;
            var nextSorts = keepExisting
                ? current.Where(sort => !string.Equals(sort.ColumnId, columnId, StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<GridSortDescriptor>();
            nextSorts.Add(new GridSortDescriptor(columnId, nextDirection));
            return nextSorts;
        }
    }
}
