using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupingController
    {
        public IReadOnlyList<GridGroupDescriptor> ApplyDrop(
            IReadOnlyList<GridGroupDescriptor> current,
            GridGroupingDragPayload payload,
            GridGroupingDropTarget target,
            int dropIndex = -1,
            GridSortDirection defaultDirection = GridSortDirection.Ascending)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (current == null)
            {
                current = Array.Empty<GridGroupDescriptor>();
            }
            var list = current.ToList();

            var existingIndex = list.FindIndex(x => x.ColumnId == payload.ColumnId);
            if (target == GridGroupingDropTarget.RemoveGrouping)
            {
                if (existingIndex >= 0)
                {
                    list.RemoveAt(existingIndex);
                }

                return list;
            }

            if (dropIndex < 0 || dropIndex > list.Count)
            {
                dropIndex = list.Count;
            }

            GridGroupDescriptor descriptor;
            if (existingIndex >= 0)
            {
                descriptor = list[existingIndex];
                list.RemoveAt(existingIndex);
                if (existingIndex < dropIndex)
                {
                    dropIndex--;
                }
            }
            else
            {
                descriptor = new GridGroupDescriptor(payload.ColumnId, defaultDirection);
            }

            list.Insert(dropIndex, descriptor);
            return list;
        }

        public IReadOnlyList<GridGroupDescriptor> ToggleDirection(IReadOnlyList<GridGroupDescriptor> current, string columnId)
        {
            if (string.IsNullOrWhiteSpace(columnId))
            {
                throw new ArgumentException("Column id is required.", nameof(columnId));
            }

            if (current == null)
            {
                current = Array.Empty<GridGroupDescriptor>();
            }
            var list = current.ToList();
            var index = list.FindIndex(x => x.ColumnId == columnId);
            if (index < 0)
            {
                return list;
            }

            var previous = list[index];
            var nextDirection = previous.Direction == GridSortDirection.Ascending
                ? GridSortDirection.Descending
                : GridSortDirection.Ascending;
            list[index] = new GridGroupDescriptor(previous.ColumnId, nextDirection);
            return list;
        }
    }
}
