using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Columns
{
    public sealed class GridLayoutState
    {
        private readonly List<GridColumnDefinition> _columns;

        public GridLayoutState(IReadOnlyList<GridColumnDefinition> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            if (columns.Count == 0)
            {
                throw new ArgumentException("At least one column is required.", nameof(columns));
            }

            _columns = columns.Select(CloneColumn).ToList();
            NormalizeDisplayIndexes();
        }

        public IReadOnlyList<GridColumnDefinition> Columns => _columns.ToArray();

        public IReadOnlyList<GridColumnDefinition> VisibleColumns => _columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToArray();

        public void ResizeColumn(string columnId, double requestedWidth)
        {
            var column = Find(columnId);
            if (requestedWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedWidth));
            }

            Replace(columnId, column.WithWidth(Math.Max(column.MinWidth, requestedWidth)));
        }

        public void SetColumnVisibility(string columnId, bool isVisible)
        {
            var column = Find(columnId);
            Replace(columnId, column.WithVisibility(isVisible));
        }

        public void SetFrozen(string columnId, bool frozen)
        {
            var column = Find(columnId);
            Replace(columnId, column.WithFrozen(frozen));
        }

        public void SetMinWidth(string columnId, double minWidth)
        {
            var column = Find(columnId);
            Replace(columnId, column.WithMinWidth(minWidth));
        }

        public void ReorderColumn(string columnId, int targetDisplayIndex)
        {
            if (targetDisplayIndex < 0 || targetDisplayIndex >= _columns.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDisplayIndex));
            }

            var ordered = _columns.OrderBy(c => c.DisplayIndex).ToList();
            var moving = ordered.Single(c => c.Id == columnId);
            ordered.Remove(moving);
            ordered.Insert(targetDisplayIndex, moving);

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i] = ordered[i].WithDisplayIndex(i);
            }

            _columns.Clear();
            _columns.AddRange(ordered);
        }

        public GridLayoutSnapshot CreateSnapshot()
        {
            return new GridLayoutSnapshot(_columns.Select(CloneColumn).ToArray());
        }

        public void ApplySnapshot(GridLayoutSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var existingById = _columns.ToDictionary(c => c.Id);
            _columns.Clear();
            foreach (var savedColumn in snapshot.Columns)
            {
                GridColumnDefinition baseline;
                if (!existingById.TryGetValue(savedColumn.Id, out baseline))
                {
                    continue;
                }

                _columns.Add(savedColumn.WithValueType(baseline.ValueType));
            }

            foreach (var missing in existingById.Values.Where(c => _columns.All(x => x.Id != c.Id)))
            {
                _columns.Add(CloneColumn(missing));
            }

            NormalizeDisplayIndexes();
        }

        public double EstimateAutoFitWidth(string columnId, IEnumerable<string> samples, double padding = 24d, double min = 40d, double max = 600d)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            Find(columnId);
            var longest = samples.Select(s => s ?? string.Empty).DefaultIfEmpty(string.Empty).Max(s => s.Length);
            var estimated = (longest * 8d) + padding;
            return Math.Max(min, Math.Min(max, estimated));
        }

        private GridColumnDefinition Find(string columnId)
        {
            var column = _columns.SingleOrDefault(c => c.Id == columnId);
            if (column == null)
            {
                throw new InvalidOperationException("Column was not found: " + columnId);
            }

            return column;
        }

        private void Replace(string columnId, GridColumnDefinition replacement)
        {
            var index = _columns.FindIndex(c => c.Id == columnId);
            if (index < 0)
            {
                throw new InvalidOperationException("Column was not found: " + columnId);
            }

            _columns[index] = CloneColumn(replacement);
        }

        private void NormalizeDisplayIndexes()
        {
            var ordered = _columns.OrderBy(c => c.DisplayIndex < 0 ? int.MaxValue : c.DisplayIndex).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i] = ordered[i].WithDisplayIndex(i);
            }

            _columns.Clear();
            _columns.AddRange(ordered);
        }

        private static GridColumnDefinition CloneColumn(GridColumnDefinition column)
        {
            return new GridColumnDefinition(
                column.Id,
                column.Header,
                column.Width,
                column.MinWidth,
                column.IsVisible,
                column.IsFrozen,
                column.IsEditable,
                column.DisplayIndex,
                column.ValueType,
                column.EditorKind,
                column.EditorItems,
                column.EditMask,
                column.ValueKind,
                column.ValidationConstraints,
                column.EditorItemsMode);
        }
    }
}
