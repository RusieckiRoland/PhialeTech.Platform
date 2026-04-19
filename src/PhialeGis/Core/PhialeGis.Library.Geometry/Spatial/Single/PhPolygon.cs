using System;
using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Geometry.Spatial.Single
{
    public sealed class PhPolygon : IPhGeometry
    {
        public PhPolygon(IList<PhPoint> outer, IList<IList<PhPoint>> holes = null)
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (outer.Count < 4) throw new ArgumentException("Outer ring must have >= 4 points (closed)");
            Outer = outer; Holes = holes ?? new List<IList<PhPoint>>(0);
            _env = ComputeEnvelope();
        }

        public IList<PhPoint> Outer { get; private set; }
        public IList<IList<PhPoint>> Holes { get; private set; }
        private PhEnvelope _env;

        public PhGeometryKind Kind => PhGeometryKind.Polygon;
        public PhEnvelope Envelope => _env;

        public void Transform(in PhMatrix2D m)
        {
            for (int i = 0; i < Outer.Count; i++) Outer[i] = m.Transform(Outer[i]);
            for (int h = 0; h < Holes.Count; h++)
            {
                var r = Holes[h];
                for (int i = 0; i < r.Count; i++) r[i] = m.Transform(r[i]);
            }
            _env = ComputeEnvelope();
        }

        public IPhGeometry Bake(in PhMatrix2D m)
        {
            var o = new PhPoint[Outer.Count];
            for (int i = 0; i < o.Length; i++) o[i] = m.Transform(Outer[i]);
            var hs = new List<IList<PhPoint>>(Holes.Count);
            for (int h = 0; h < Holes.Count; h++)
            {
                var r = Holes[h];
                var arr = new PhPoint[r.Count];
                for (int i = 0; i < arr.Length; i++) arr[i] = m.Transform(r[i]);
                hs.Add(arr);
            }
            return new PhPolygon(o, hs);
        }

        public static void EnsureClosed(IList<PhPoint> ring)
        {
            if (ring.Count == 0) return;
            var first = ring[0]; var last = ring[ring.Count - 1];
            if (first.X != last.X || first.Y != last.Y) ring.Add(first);
        }

        public static double SignedArea(IList<PhPoint> ring)
        {
            double s = 0; int j = ring.Count - 1;
            for (int i = 0; i < ring.Count; i++)
            { s += (ring[j].X * ring[i].Y - ring[i].X * ring[j].Y); j = i; }
            return 0.5 * s;
        }

        public static void EnsureOrientation(IList<PhPoint> ring, bool desiredCcw)
        {
            bool ccw = SignedArea(ring) > 0;
            if (ccw != desiredCcw) ReverseInPlace(ring);
        }

        private static void ReverseInPlace(IList<PhPoint> ring)
        {
            int i = 0, j = ring.Count - 1;
            while (i < j) { var t = ring[i]; ring[i] = ring[j]; ring[j] = t; i++; j--; }
        }

        public bool HitTest(PhPoint p)
        {
            if (!Envelope.Contains(p)) return false;
            if (!PointInRing(p, Outer)) return false;
            for (int h = 0; h < Holes.Count; h++) if (PointInRing(p, Holes[h])) return false;
            return true;
        }

        private static bool PointInRing(PhPoint p, IList<PhPoint> ring)
        {
            bool inside = false; int j = ring.Count - 1;
            for (int i = 0; i < ring.Count; i++)
            {
                var pi = ring[i]; var pj = ring[j];
                bool inter = ((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                             (p.X <= (pj.X - pi.X) * (p.Y - pi.Y) / ((pj.Y - pi.Y) + 1e-12) + pi.X);
                if (inter) inside = !inside; j = i;
            }
            return inside;
        }

        private PhEnvelope ComputeEnvelope()
        {
            var e = PhEnvelope.Empty;
            for (int i = 0; i < Outer.Count; i++) e.Include(Outer[i]);
            for (int h = 0; h < Holes.Count; h++) { var r = Holes[h]; for (int i = 0; i < r.Count; i++) e.Include(r[i]); }
            return e;
        }
    }
}
