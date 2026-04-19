using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Components
{
    /// <summary>
    /// Polygon geometry (single outer ring, model space).
    /// </summary>
    public sealed class PolygonGeom : IGeometry
    {
        public List<Vec2> Ring { get; private set; }

        public PolygonGeom()
        {
            Ring = new List<Vec2>();
        }

        public PolygonGeom(IEnumerable<Vec2> ring)
        {
            Ring = new List<Vec2>(ring ?? new Vec2[0]);
        }
    }
}
