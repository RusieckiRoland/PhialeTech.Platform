namespace PhialeGis.Library.Core.Models.Parsing
{
    internal enum TokenType
    {
        Point,
        LineString,
        Polygon,
        MultiPoint,
        MultiLineString,
        MultiPolygon,
        GeometryCollection,
        Number,
        LeftParen,
        RightParen,
        Comma,
        Whitespace,
        Semicolon,
        Zcoord,
        Mcoord,
        Zmcoord,
        Invalid
    }
}