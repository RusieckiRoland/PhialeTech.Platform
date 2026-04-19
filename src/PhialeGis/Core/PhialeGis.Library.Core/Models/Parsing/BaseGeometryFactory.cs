using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Models.Geometry.Interfaces;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class BaseGeometryFactory : IGeometryFactory
    {
        public IGeometry BuildGeometry(List<PhPoint> points, List<int> parts)
        {
            var vector = new PhVector(points, parts);
            return vector;
        }
    }
}