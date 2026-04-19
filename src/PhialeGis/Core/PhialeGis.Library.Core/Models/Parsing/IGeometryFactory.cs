using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Models.Geometry.Interfaces;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal interface IGeometryFactory
    {
        IGeometry BuildGeometry(List<PhPoint> points, List<int> parts);
    }
}