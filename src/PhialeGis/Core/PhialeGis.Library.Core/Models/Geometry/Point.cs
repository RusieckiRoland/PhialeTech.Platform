namespace PhialeGis.Library.Core.Models.Geometry
{
    public struct Point
    {
        internal double X { get; set; }
        internal double Y { get; set; }

        internal Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"{{X={X}, Y={Y}}}";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point)) return false;
            Point p = (Point)obj;
            return X == p.X && Y == p.Y;
        }

        // .net standard 2.0 own implementation
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Point p1, Point p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !p1.Equals(p2);
        }
    }
}