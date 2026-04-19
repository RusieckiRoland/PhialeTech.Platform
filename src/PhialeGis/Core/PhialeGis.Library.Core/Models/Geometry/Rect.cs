using System;

namespace PhialeGis.Library.Core.Models.Geometry
{
    public struct Rect : IEquatable<Rect>
    {
        internal double X { get; }
        internal double Y { get; }
        internal double Width { get; }
        internal double Height { get; }

        internal double Left => X;
        internal double Top => Y;
        internal double Right => X + Width;
        internal double Bottom => Y + Height;

        internal bool IsEmpty => Width <= 0 || Height <= 0;
        internal double Area => IsEmpty ? 0 : Width * Height;

        internal Rect(double x, double y, double width, double height)
        {
            if (width < 0 || height < 0)
                throw new ArgumentException("Width and Height must be non-negative.");

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal Rect(Point p1, Point p2)
        {
            X = Math.Min(p1.X, p2.X);
            Y = Math.Min(p1.Y, p2.Y);
            Width = Math.Abs(p2.X - p1.X);
            Height = Math.Abs(p2.Y - p1.Y);
        }

        internal bool Contains(double x, double y)
        {
            return (x >= Left && x < Right && y >= Top && y < Bottom);
        }

        internal bool IntersectsWith(Rect rect)
        {
            return (rect.Left < Right && Left < rect.Right && rect.Top < Bottom && Top < rect.Bottom);
        }

        public override string ToString()
        {
            return $"{{X={X}, Y={Y}, Width={Width}, Height={Height}}}";
        }

        public bool Equals(Rect other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is Rect other && Equals(other);
        }

        // Own implementation because .net standard 2.0;
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + Width.GetHashCode();
                hash = hash * 23 + Height.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Rect left, Rect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect left, Rect right)
        {
            return !(left == right);
        }
    }
}