using System.Collections.Generic;

namespace PhialeGis.Library.Geometry.Abstractions
{
    public interface IPhFeature
    {
        long Id { get; set; }
        IPhGeometry Geometry { get; set; }
        IDictionary<string, object> Attributes { get; }
    }
}
