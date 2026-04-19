using System;
using System.Runtime.CompilerServices;

namespace PhialeGis.Library.Geometry.Spatial.Primitives
{
    public struct PhMatrix2D
    {
        public double M11, M12, M13; // x' = M11*x + M12*y + M13
        public double M21, M22, M23; // y' = M21*x + M22*y + M23

        public static PhMatrix2D Identity => new PhMatrix2D { M11 = 1, M22 = 1 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PhMatrix2D Translate(double dx, double dy) =>
            new PhMatrix2D { M11 = 1, M22 = 1, M13 = dx, M23 = dy };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PhMatrix2D Scale(double sx, double sy) =>
            new PhMatrix2D { M11 = sx, M22 = sy };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PhMatrix2D Rotate(double rad)
        {
            var c = Math.Cos(rad); var s = Math.Sin(rad);
            return new PhMatrix2D { M11 = c, M12 = -s, M21 = s, M22 = c };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PhMatrix2D Multiply(in PhMatrix2D a, in PhMatrix2D b) =>
            new PhMatrix2D
            {
                M11 = a.M11 * b.M11 + a.M12 * b.M21,
                M12 = a.M11 * b.M12 + a.M12 * b.M22,
                M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13,
                M21 = a.M21 * b.M11 + a.M22 * b.M21,
                M22 = a.M21 * b.M12 + a.M22 * b.M22,
                M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PhPoint Transform(PhPoint p) =>
            new PhPoint(M11 * p.X + M12 * p.Y + M13, M21 * p.X + M22 * p.Y + M23);

        public bool IsIdentity => M11 == 1 && M22 == 1 && M12 == 0 && M21 == 0 && M13 == 0 && M23 == 0;
    }
}
