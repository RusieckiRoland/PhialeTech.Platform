using System;

namespace PhialeGis.Library.Core.Components
{
    /// <summary>
    /// 2D transform in model space.
    /// </summary>
    public struct Transform2D
    {
        public double X;
        public double Y;
        public double RotationDeg;
        public double Scale;

        public static Transform2D Identity()
        {
            return new Transform2D { X = 0, Y = 0, RotationDeg = 0, Scale = 1.0 };
        }
    }
}
