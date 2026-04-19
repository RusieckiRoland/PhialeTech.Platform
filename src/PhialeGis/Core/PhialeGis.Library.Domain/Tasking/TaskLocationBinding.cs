using PhialeGis.Library.Geometry.Abstractions;

namespace PhialeGis.Library.Domain.Tasking
{
    public sealed class TaskLocationBinding
    {
        public string LayerName { get; set; }
        public long? FeatureId { get; set; }
        public IPhGeometry Geometry { get; set; }
    }
}
