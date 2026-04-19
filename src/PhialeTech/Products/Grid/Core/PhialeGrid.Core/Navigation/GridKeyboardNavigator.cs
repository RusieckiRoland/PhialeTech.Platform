using System;
using PhialeGrid.Core.Input;

namespace PhialeGrid.Core.Navigation
{
    public enum GridNavigationKey
    {
        Left,
        Right,
        Up,
        Down,
        Tab,
        Enter,
    }

    public sealed class GridKeyboardNavigator
    {
        private readonly int _rowCount;
        private readonly int _columnCount;

        public GridKeyboardNavigator(int rowCount, int columnCount)
        {
            if (rowCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount));
            }

            if (columnCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            }

            _rowCount = rowCount;
            _columnCount = columnCount;
        }

        public GridCellPosition Move(GridCellPosition current, GridNavigationKey key)
        {
            var row = current.Row;
            var col = current.Column;

            switch (key)
            {
                case GridNavigationKey.Left:
                    col--;
                    break;
                case GridNavigationKey.Right:
                case GridNavigationKey.Tab:
                    col++;
                    break;
                case GridNavigationKey.Up:
                    row--;
                    break;
                case GridNavigationKey.Down:
                case GridNavigationKey.Enter:
                    row++;
                    break;
            }

            row = Clamp(row, 0, Math.Max(0, _rowCount - 1));
            col = Clamp(col, 0, Math.Max(0, _columnCount - 1));
            return new GridCellPosition(row, col);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
