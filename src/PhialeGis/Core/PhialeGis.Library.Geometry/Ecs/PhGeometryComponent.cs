using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Ecs
{
    public sealed class PhGeometryComponent : IPhComponent
    {
        public IPhGeometry Geometry;
        public PhMatrix2D LocalTransform;   // transient (preview)
        public bool UseBakeOnCommit;        // if true, editor commits to coords
    }
}
