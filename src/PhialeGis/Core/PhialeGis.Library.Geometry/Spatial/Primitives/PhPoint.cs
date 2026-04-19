using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PhialeGis.Library.Geometry.Spatial.Primitives
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PhPoint
    {
        public double X;
        public double Y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PhPoint(double x, double y) { X = x; Y = y; }
        public override string ToString() => $"({X}, {Y})";
    }
}
