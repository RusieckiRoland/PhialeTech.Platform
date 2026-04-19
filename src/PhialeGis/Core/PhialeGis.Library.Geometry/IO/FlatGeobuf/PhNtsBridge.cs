using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Multi;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

#if PH_FGB
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;
#endif

namespace PhialeGis.Library.Geometry.IO.FlatGeobuf
{
#if PH_FGB
    public static class PhNtsBridge
    {
        private static readonly GeometryFactory _gf = new GeometryFactory();

        public static NtsGeometry ToNts(IPhGeometry g)
        {
            switch (g.Kind)
            {
                case PhGeometryKind.Point:
                    var p = (PhPointEntity)g; return _gf.CreatePoint(new Coordinate(p.Point.X, p.Point.Y));

                case PhGeometryKind.LineString:
                    var ln = (PhPolyLine)g; return _gf.CreateLineString(ToCoords(ln.Points));

                case PhGeometryKind.Polygon:
                    var pg = (PhPolygon)g; return _gf.CreatePolygon(ToRing(pg.Outer), ToHoles(pg.Holes));

                case PhGeometryKind.MultiPoint:
                    var mp = (PhMultiPoint)g; return _gf.CreateMultiPointFromCoords(ToCoords(mp.Points));

                case PhGeometryKind.MultiLineString:
                    var mls = (PhMultiLineString)g;
                    var lines = new LineString[mls.Lines.Count];
                    for (int i = 0; i < lines.Length; i++)
                        lines[i] = _gf.CreateLineString(ToCoords(mls.Lines[i].Points));
                    return _gf.CreateMultiLineString(lines);

                case PhGeometryKind.MultiPolygon:
                    var mpoly = (PhMultiPolygon)g;
                    var polys = new Polygon[mpoly.Polygons.Count];
                    for (int i = 0; i < polys.Length; i++)
                        polys[i] = _gf.CreatePolygon(ToRing(mpoly.Polygons[i].Outer),
                                                     ToHoles(mpoly.Polygons[i].Holes));
                    return _gf.CreateMultiPolygon(polys);

                case PhGeometryKind.GeometryCollection:
                    var gc = (PhGeometryCollection)g;
                    var arr = new NtsGeometry[gc.Geometries.Count];
                    for (int i = 0; i < arr.Length; i++) arr[i] = ToNts(gc.Geometries[i]);
                    return _gf.CreateGeometryCollection(arr);
            }
            return null;
        }

        public static IPhGeometry FromNts(NtsGeometry g)
        {
            switch (g)
            {
                case Point p:
                    return new PhPointEntity(new PhPoint(p.X, p.Y));

                case LineString ls:
                    return new PhPolyLine(FromCoords(ls.Coordinates));

                case Polygon pg:
                    return new PhPolygon(FromCoords(pg.ExteriorRing.Coordinates),
                                         FromRings(pg.Holes));

                case MultiPoint mp:
                    return new PhMultiPoint(FromCoords(mp.Coordinates));

                case MultiLineString mls:
                    var lines = new List<PhPolyLine>(mls.NumGeometries);
                    for (int i = 0; i < mls.NumGeometries; i++)
                        lines.Add((PhPolyLine)FromNts((LineString)mls.GetGeometryN(i)));
                    return new PhMultiLineString(lines);

                case MultiPolygon mpoly:
                    var polys = new List<PhPolygon>(mpoly.NumGeometries);
                    for (int i = 0; i < mpoly.NumGeometries; i++)
                        polys.Add((PhPolygon)FromNts((Polygon)mpoly.GetGeometryN(i)));
                    return new PhMultiPolygon(polys);

                case NetTopologySuite.Geometries.GeometryCollection coll:
                    var list = new List<IPhGeometry>(coll.NumGeometries);
                    for (int i = 0; i < coll.NumGeometries; i++)
                        list.Add(FromNts(coll.GetGeometryN(i)));
                    return new PhGeometryCollection(list);
            }
            return null;
        }

        static Coordinate[] ToCoords(System.Collections.Generic.IList<PhPoint> pts)
        {
            var arr = new Coordinate[pts.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = new Coordinate(pts[i].X, pts[i].Y);
            return arr;
        }

        static System.Collections.Generic.IList<PhPoint> FromCoords(Coordinate[] c)
        {
            var list = new System.Collections.Generic.List<PhPoint>(c.Length);
            for (int i = 0; i < c.Length; i++) list.Add(new PhPoint(c[i].X, c[i].Y));
            return list;
        }

        static LinearRing ToRing(System.Collections.Generic.IList<PhPoint> pts) => _gf.CreateLinearRing(ToCoords(pts));

        static LinearRing[] ToHoles(System.Collections.Generic.IList<System.Collections.Generic.IList<PhPoint>> holes)
        {
            var arr = new LinearRing[holes.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = ToRing(holes[i]);
            return arr;
        }

        static System.Collections.Generic.IList<System.Collections.Generic.IList<PhPoint>> FromRings(LinearRing[] rings)
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.IList<PhPoint>>(rings.Length);
            for (int i = 0; i < rings.Length; i++) list.Add(FromCoords(rings[i].Coordinates));
            return list;
        }
    }
#endif
}
