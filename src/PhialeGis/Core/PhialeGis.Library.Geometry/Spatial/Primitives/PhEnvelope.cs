using System.Runtime.CompilerServices;

namespace PhialeGis.Library.Geometry.Spatial.Primitives
{
    public struct PhEnvelope
    {
        public double MinX, MinY, MaxX, MaxY;

        public PhEnvelope(double minX, double minY, double maxX, double maxY)
        { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }

        public static PhEnvelope Empty =>
            new PhEnvelope(double.PositiveInfinity, double.PositiveInfinity,
                           double.NegativeInfinity, double.NegativeInfinity);

        public bool IsEmpty => MinX > MaxX || MinY > MaxY;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Include(PhPoint p)
        {
            if (p.X < MinX) MinX = p.X; if (p.X > MaxX) MaxX = p.X;
            if (p.Y < MinY) MinY = p.Y; if (p.Y > MaxY) MaxY = p.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Include(PhEnvelope e)
        {
            if (e.IsEmpty) return;
            if (e.MinX < MinX) MinX = e.MinX; if (e.MaxX > MaxX) MaxX = e.MaxX;
            if (e.MinY < MinY) MinY = e.MinY; if (e.MaxY > MaxY) MaxY = e.MaxY;
        }

        public bool Contains(PhPoint p) =>
            p.X >= MinX && p.X <= MaxX && p.Y >= MinY && p.Y <= MaxY;
    }
}
