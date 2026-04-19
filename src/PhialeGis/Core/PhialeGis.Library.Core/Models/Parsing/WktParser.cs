using PhialeGis.Library.Core.Models.Geometry.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class WKTParser
    {
        private readonly IGeometryFactory _geometryFactory;

        internal WKTParser(IGeometryFactory geometryFactory)
        {
            _geometryFactory = geometryFactory;
        }

        internal List<TokenStruct> Tokenize(string wkt)
        {
            // This pattern matches parentheses, numbers, text for geometry types, and commas.
            // It assumes that the input WKT is correctly formatted.
            const string pattern = @"(\()|(\))|(\s*,\s*)|(-?\d+(\.\d+)?)|([a-zA-Z]+)";

            var tokens = new List<TokenStruct>();

            var matches = Regex.Matches(wkt, pattern);

            foreach (Match match in matches)
            {
                var token = new TokenStruct(match.Value);
                tokens.Add(token);
            }

            return tokens;
        }

        internal void ParseGeometry(Context context)
        {
            switch (context.CurrentToken.Type)
            {
                case TokenType.Point:
                    var pointExpression = new PointExpression();
                    pointExpression.Interpret(context);
                    break;

                case TokenType.LineString:
                    var lineStringExpression = new LineStringExpression();
                    lineStringExpression.Interpret(context);
                    break;

                case TokenType.Polygon:
                    var polygonExpression = new PolygonExpression();
                    polygonExpression.Interpret(context);
                    break;
            }
        }

        internal IGeometry ProduceGeometry(Context context)
        {
            return _geometryFactory.BuildGeometry(context.Points, context.Parts);
        }
    }
}