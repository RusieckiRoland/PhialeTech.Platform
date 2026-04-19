using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;

namespace PhialeGis.Library.Geometry.Features
{
    public sealed class PhFeature : IPhFeature
    {
        public long Id { get; set; }
        public IPhGeometry Geometry { get; set; }
        public IDictionary<string, object> Attributes { get; }

        public PhFeature(long id, IPhGeometry geometry)
        {
            Id = id; Geometry = geometry;
            Attributes = new Dictionary<string, object>();
        }
    }
}
