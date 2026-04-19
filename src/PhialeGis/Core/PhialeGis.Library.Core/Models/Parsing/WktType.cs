namespace PhialeGis.Library.Core.Models.Parsing
{
    internal enum WktType
    {
        None,
        Point,
        LineString,
        Polygon,
        Triangle,
        PolyhedralSurface,
        Tin,
        MultiPoint,
        MultiLineString,
        MultiPolygon,
        GeometryCollection,
    }
}