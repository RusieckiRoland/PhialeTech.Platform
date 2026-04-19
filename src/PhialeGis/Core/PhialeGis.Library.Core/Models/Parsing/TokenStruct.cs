using System.Globalization;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal struct TokenStruct
    {
        internal string Value { get; set; }
        internal TokenType Type { get; set; }

        internal TokenStruct(string value)
        {
            Value = value;
            Type = ValueToTokenType(value);
        }

        internal static TokenType ValueToTokenType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return TokenType.Invalid;
            }

            string normalizedValue = value.Trim().ToLowerInvariant();

            switch (normalizedValue)
            {
                case "point":
                    return TokenType.Point;

                case "linestring":
                    return TokenType.LineString;

                case "polygon":
                    return TokenType.Polygon;

                case "multipoint":
                    return TokenType.MultiPoint;

                case "multilinestring":
                    return TokenType.MultiLineString;

                case "multipolygon":
                    return TokenType.MultiPolygon;

                case "geometrycollection":
                    return TokenType.GeometryCollection;

                case "(":
                    return TokenType.LeftParen;

                case ")":
                    return TokenType.RightParen;

                case ",":
                    return TokenType.Comma;

                case ";":
                    return TokenType.Semicolon;

                case "z":
                    return TokenType.Zcoord;

                case "m":
                    return TokenType.Mcoord;

                case "zm":
                case "mz":
                    return TokenType.Zmcoord;

                default:

                    if (double.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                    {
                        return TokenType.Number;
                    }

                    if (string.IsNullOrWhiteSpace(normalizedValue))
                    {
                        return TokenType.Whitespace;
                    }

                    return TokenType.Invalid;
            }
        }
    }
}