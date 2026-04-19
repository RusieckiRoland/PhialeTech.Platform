using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Spatial.Single
{
    public sealed class PhPointEntity : IPhGeometry
    {
        public PhPointEntity(PhPoint p) { Point = p; _env = new PhEnvelope(p.X, p.Y, p.X, p.Y); }

        public PhPoint Point { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.Point;
        public PhEnvelope Envelope => _env;

        public void Transform(in PhMatrix2D m)
        {
            Point = m.Transform(Point);
            _env = new PhEnvelope(Point.X, Point.Y, Point.X, Point.Y);
        }

        public IPhGeometry Bake(in PhMatrix2D m) => new PhPointEntity(m.Transform(Point));
    }
}
