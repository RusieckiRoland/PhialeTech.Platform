// PhialeGis.Library.Geometry/Utils/PhBoundingBox.cs
using System.Collections.Generic;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;
using PhialeGis.Library.Geometry.Spatial.Multi;

namespace PhialeGis.Library.Geometry.Utils
{
    public struct PhBBox
    {
        public double MinX, MinY, MaxX, MaxY;

        public bool IsEmpty =>
            double.IsPositiveInfinity(MinX) || double.IsPositiveInfinity(MinY) ||
            double.IsNegativeInfinity(MaxX) || double.IsNegativeInfinity(MaxY);

        public static PhBBox Empty() => new PhBBox
        {
            MinX = double.PositiveInfinity,
            MinY = double.PositiveInfinity,
            MaxX = double.NegativeInfinity,
            MaxY = double.NegativeInfinity
        };

        public void Expand(PhPoint p)
        {
            if (p.X < MinX) MinX = p.X;
            if (p.Y < MinY) MinY = p.Y;
            if (p.X > MaxX) MaxX = p.X;
            if (p.Y > MaxY) MaxY = p.Y;
        }

        public void Union(ref PhBBox other)
        {
            if (other.IsEmpty) return;
            if (IsEmpty) { this = other; return; }
            if (other.MinX < MinX) MinX = other.MinX;
            if (other.MinY < MinY) MinY = other.MinY;
            if (other.MaxX > MaxX) MaxX = other.MaxX;
            if (other.MaxY > MaxY) MaxY = other.MaxY;
        }
    }

    public static class PhBoundingBox
    {
        /// <summary>Compute AABB for a single geometry (Single/Multi/Collection).</summary>
        public static bool TryCompute(IPhGeometry g, out PhBBox bbox)
        {
            bbox = PhBBox.Empty();
            if (g == null) return false;

            switch (g.Kind)
            {
                case PhGeometryKind.Point:
                    {
                        var p = ((PhPointEntity)g).Point;
                        bbox.MinX = bbox.MaxX = p.X;
                        bbox.MinY = bbox.MaxY = p.Y;
                        return true;
                    }
                case PhGeometryKind.LineString:
                    return TryFromPoints(((PhPolyLine)g).Points, ref bbox);

                case PhGeometryKind.Polygon:
                    {
                        var poly = (PhPolygon)g;
                        bool ok = TryFromPoints(poly.Outer, ref bbox);
                        if (poly.Holes != null)
                            for (int i = 0; i < poly.Holes.Count; i++)
                                ok |= TryFromPoints(poly.Holes[i], ref bbox);
                        return ok;
                    }

                case PhGeometryKind.MultiPoint:
                    return TryFromPoints(((PhMultiPoint)g).Points, ref bbox);

                case PhGeometryKind.MultiLineString:
                    {
                        var ml = (PhMultiLineString)g;
                        bool any = false;
                        for (int i = 0; i < ml.Lines.Count; i++)
                            any |= TryFromPoints(ml.Lines[i].Points, ref bbox);
                        return any;
                    }

                case PhGeometryKind.MultiPolygon:
                    {
                        var mp = (PhMultiPolygon)g;
                        bool any = false;
                        for (int i = 0; i < mp.Polygons.Count; i++)
                        {
                            var poly = mp.Polygons[i];
                            any |= TryFromPoints(poly.Outer, ref bbox);
                            if (poly.Holes != null)
                                for (int h = 0; h < poly.Holes.Count; h++)
                                    any |= TryFromPoints(poly.Holes[h], ref bbox);
                        }
                        return any;
                    }

                case PhGeometryKind.GeometryCollection:
                    {
                        var gc = (PhGeometryCollection)g;
                        bool any = false;
                        for (int i = 0; i < gc.Geometries.Count; i++)
                        {
                            if (TryCompute(gc.Geometries[i], out var child))
                            {
                                bbox.Union(ref child);
                                any = true;
                            }
                        }
                        return any;
                    }

                default: return false;
            }
        }

        /// <summary>Compute AABB for a collection of geometries.</summary>
        public static bool TryCompute(IEnumerable<IPhGeometry> geoms, out PhBBox bbox)
        {
            bbox = PhBBox.Empty();
            if (geoms == null) return false;

            bool any = false;
            foreach (var g in geoms)
            {
                if (g == null) continue;
                if (TryCompute(g, out var gb))
                {
                    bbox.Union(ref gb);
                    any = true;
                }
            }
            return any && !bbox.IsEmpty;
        }

        private static bool TryFromPoints(IList<PhPoint> pts, ref PhBBox bbox)
        {
            if (pts == null || pts.Count == 0) return false;

            if (bbox.IsEmpty)
            {
                bbox.MinX = bbox.MaxX = pts[0].X;
                bbox.MinY = bbox.MaxY = pts[0].Y;
            }

            for (int i = 0; i < pts.Count; i++)
                bbox.Expand(pts[i]);

            return true;
        }
    }
}
