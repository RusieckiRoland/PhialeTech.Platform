using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Components
{
    /// <summary>
    /// Polyline geometry (model space).
    /// </summary>
    public sealed class PolylineGeom : IGeometry
    {
        public List<Vec2> Points { get; private set; }

        public PolylineGeom()
        {
            Points = new List<Vec2>();
        }

        public PolylineGeom(IEnumerable<Vec2> pts)
        {
            Points = new List<Vec2>(pts ?? new Vec2[0]);
        }
    }
}
