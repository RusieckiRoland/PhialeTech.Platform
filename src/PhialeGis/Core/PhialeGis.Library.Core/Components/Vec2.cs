using System;

namespace PhialeGis.Library.Core.Components
{
    /// <summary>
    /// Simple 2D point/vector in model space.
    /// </summary>
    public struct Vec2
    {
        public double X;
        public double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
