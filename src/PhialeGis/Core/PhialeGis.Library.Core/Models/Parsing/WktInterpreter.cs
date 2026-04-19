using PhialeGis.Library.Core.Models.Geometry.Interfaces;
using System;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal class WktInterpreter : IWKTInterpreter
    {
        private readonly WKTParser _wktParser;

        internal WktInterpreter(WKTParser wktParser)
        {
            _wktParser = wktParser;
        }

        public IGeometry CreateGeometryFromWKT(string wkt)
        {
            return Interpret(wkt);
        }

        private IGeometry Interpret(string wkt)
        {
            if (string.IsNullOrWhiteSpace(wkt))
                throw new ArgumentException("Input WKT string cannot be null or whitespace.", nameof(wkt));

            var tokens = _wktParser.Tokenize(wkt);

            var context = new Context(tokens);

            _wktParser.ParseGeometry(context);

            return _wktParser.ProduceGeometry(context);
        }

        public void LoadGeometryFromWKT(IGeometry geometry, string wkt)
        {
            throw new NotImplementedException();
        }
    }
}