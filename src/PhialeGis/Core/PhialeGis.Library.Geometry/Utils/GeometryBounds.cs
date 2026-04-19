using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Multi;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Geometry.Utils
{
    public static class GeometryBounds
    {
        public static bool TryGet(IPhGeometry g, out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = minY = maxX = maxY = 0;
            if (g == null) return false;

            switch (g.Kind)
            {
                case PhGeometryKind.Point:
                    {
                        var p = ((PhPointEntity)g).Point;
                        minX = maxX = p.X; minY = maxY = p.Y; return true;
                    }
                case PhGeometryKind.LineString:
                    return FromPoints(((PhPolyLine)g).Points, out minX, out minY, out maxX, out maxY);

                case PhGeometryKind.Polygon:
                    {
                        var pg = (PhPolygon)g;
                        return FromPoints(pg.Outer, out minX, out minY, out maxX, out maxY);
                    }
                case PhGeometryKind.MultiPoint:
                    {
                        var mp = (PhMultiPoint)g;
                        return FromPoints(mp.Points, out minX, out minY, out maxX, out maxY);
                    }
                case PhGeometryKind.MultiLineString:
                    {
                        var ml = (PhMultiLineString)g;
                        bool any = false;
                        foreach (var ln in ml.Lines)
                        {
                            if (FromPoints(ln.Points, out var x1, out var y1, out var x2, out var y2))
                            {
                                Acc(ref minX, ref minY, ref maxX, ref maxY, x1, y1, x2, y2, ref any);
                            }
                        }
                        return any;
                    }
                case PhGeometryKind.MultiPolygon:
                    {
                        var mp = (PhMultiPolygon)g;
                        bool any = false;
                        foreach (var p in mp.Polygons)
                        {
                            if (FromPoints(p.Outer, out var x1, out var y1, out var x2, out var y2))
                            {
                                Acc(ref minX, ref minY, ref maxX, ref maxY, x1, y1, x2, y2, ref any);
                            }
                        }
                        return any;
                    }
                case PhGeometryKind.GeometryCollection:
                    {
                        var gc = (PhGeometryCollection)g;
                        bool any = false;
                        for (int i = 0; i < gc.Geometries.Count; i++)
                        {
                            if (TryGet(gc.Geometries[i], out var x1, out var y1, out var x2, out var y2))
                            {
                                Acc(ref minX, ref minY, ref maxX, ref maxY, x1, y1, x2, y2, ref any);
                            }
                        }
                        return any;
                    }
                default:
                    return false;
            }
        }

        private static bool FromPoints(System.Collections.Generic.IList<PhPoint> pts,
                                       out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = minY = maxX = maxY = 0;
            if (pts == null || pts.Count == 0) return false;
            minX = maxX = pts[0].X; minY = maxY = pts[0].Y;
            for (int i = 1; i < pts.Count; i++)
            {
                var p = pts[i];
                if (p.X < minX) minX = p.X; else if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y; else if (p.Y > maxY) maxY = p.Y;
            }
            return true;
        }

        private static void Acc(ref double minX, ref double minY, ref double maxX, ref double maxY,
                                double x1, double y1, double x2, double y2, ref bool any)
        {
            if (!any) { minX = x1; minY = y1; maxX = x2; maxY = y2; any = true; return; }
            if (x1 < minX) minX = x1; if (y1 < minY) minY = y1;
            if (x2 > maxX) maxX = x2; if (y2 > maxY) maxY = y2;
        }
    }
}
