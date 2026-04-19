using System;

namespace PhialeGrid.Core.Input
{
    public readonly struct GridCellPosition : IEquatable<GridCellPosition>
    {
        public GridCellPosition(int row, int column)
        {
            if (row < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            if (column < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            Row = row;
            Column = column;
        }

        public int Row { get; }

        public int Column { get; }

        public bool Equals(GridCellPosition other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCellPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Row * 397) ^ Column;
        }
    }
}
