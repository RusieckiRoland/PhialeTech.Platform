using System.Collections.Generic;

namespace PhialeGrid.Core.Input
{
    public sealed class GridSelectionState
    {
        private readonly HashSet<GridCellPosition> _selectedCells = new HashSet<GridCellPosition>();

        public GridCellPosition? ActiveCell { get; private set; }

        public IReadOnlyCollection<GridCellPosition> SelectedCells => _selectedCells;

        internal void Clear()
        {
            _selectedCells.Clear();
        }

        internal void SetSingle(GridCellPosition cell)
        {
            _selectedCells.Clear();
            _selectedCells.Add(cell);
            ActiveCell = cell;
        }

        internal void Toggle(GridCellPosition cell)
        {
            if (_selectedCells.Contains(cell))
            {
                _selectedCells.Remove(cell);
            }
            else
            {
                _selectedCells.Add(cell);
            }

            ActiveCell = cell;
        }

        internal void ReplaceWithRange(GridCellPosition from, GridCellPosition to)
        {
            _selectedCells.Clear();
            foreach (var cell in EnumerateRect(from, to))
            {
                _selectedCells.Add(cell);
            }

            ActiveCell = to;
        }

        private static IEnumerable<GridCellPosition> EnumerateRect(GridCellPosition from, GridCellPosition to)
        {
            var rowStart = from.Row < to.Row ? from.Row : to.Row;
            var rowEnd = from.Row > to.Row ? from.Row : to.Row;
            var colStart = from.Column < to.Column ? from.Column : to.Column;
            var colEnd = from.Column > to.Column ? from.Column : to.Column;

            for (var row = rowStart; row <= rowEnd; row++)
            {
                for (var col = colStart; col <= colEnd; col++)
                {
                    yield return new GridCellPosition(row, col);
                }
            }
        }
    }
}
