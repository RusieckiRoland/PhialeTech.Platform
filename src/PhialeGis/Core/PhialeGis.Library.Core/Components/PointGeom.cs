using System;

namespace PhialeGis.Library.Core.Components
{
    /// <summary>
    /// Point geometry (model space).
    /// </summary>
    public struct PointGeom : IGeometry
    {
        public Vec2 P;

        public PointGeom(double x, double y)
        {
            P = new Vec2(x, y);
        }
    }
}
