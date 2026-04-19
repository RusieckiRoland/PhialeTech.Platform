using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Spatial.Multi
{
    public sealed class PhMultiPoint : IPhGeometry
    {
        public PhMultiPoint(IList<PhPoint> points)
        {
            Points = points ?? new List<PhPoint>(0);
            _env = PhEnvelope.Empty; for (int i = 0; i < Points.Count; i++) _env.Include(Points[i]);
        }

        public IList<PhPoint> Points { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.MultiPoint;
        public PhEnvelope Envelope => _env;

        public void Transform(in PhMatrix2D m)
        {
            for (int i = 0; i < Points.Count; i++) Points[i] = m.Transform(Points[i]);
            _env = PhEnvelope.Empty; for (int i = 0; i < Points.Count; i++) _env.Include(Points[i]);
        }

        public IPhGeometry Bake(in PhMatrix2D m)
        {
            var arr = new PhPoint[Points.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = m.Transform(Points[i]);
            return new PhMultiPoint(arr);
        }
    }
}
