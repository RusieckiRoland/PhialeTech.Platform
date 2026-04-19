using System;

namespace PhialeGrid.Core.Virtualization
{
    public readonly struct GridRange : IEquatable<GridRange>
    {
        public GridRange(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Start = start;
            Length = length;
        }

        public int Start { get; }

        public int Length { get; }

        public int EndExclusive => Start + Length;

        public bool Equals(GridRange other)
        {
            return Start == other.Start && Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            return obj is GridRange range && Equals(range);
        }

        public override int GetHashCode()
        {
            return (Start * 397) ^ Length;
        }
    }
}
