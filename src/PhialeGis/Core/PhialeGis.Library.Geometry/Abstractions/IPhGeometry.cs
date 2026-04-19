using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Abstractions
{
    public interface IPhGeometry
    {
        PhGeometryKind Kind { get; }
        PhEnvelope Envelope { get; }
        void Transform(in PhMatrix2D m);
        IPhGeometry Bake(in PhMatrix2D m);
    }
}
