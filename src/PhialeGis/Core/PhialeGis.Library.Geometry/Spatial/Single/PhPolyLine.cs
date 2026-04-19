using System;
using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Spatial.Single
{
    public sealed class PhPolyLine : IPhGeometry
    {
        public PhPolyLine(IList<PhPoint> points)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            if (Points.Count < 2) throw new ArgumentException("Polyline requires >= 2 points");
            _env = PhEnvelope.Empty; for (int i = 0; i < Points.Count; i++) _env.Include(Points[i]);
        }

        public IList<PhPoint> Points { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.LineString;
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
            return new PhPolyLine(arr);
        }

        public bool HitTest(PhPoint p, double aperture)
        {
            double a2 = aperture * aperture;
            for (int i = 1; i < Points.Count; i++)
                if (Distance2(p, Points[i - 1], Points[i]) <= a2) return true;
            return false;
        }

        private static double Distance2(PhPoint p, PhPoint a, PhPoint b)
        {
            double abx = b.X - a.X, aby = b.Y - a.Y;
            double apx = p.X - a.X, apy = p.Y - a.Y;
            double t = (abx * apx + aby * apy) / (abx * abx + aby * aby);
            if (t < 0) t = 0; else if (t > 1) t = 1;
            double cx = a.X + t * abx, cy = a.Y + t * aby;
            double dx = p.X - cx, dy = p.Y - cy; return dx * dx + dy * dy;
        }
    }
}
