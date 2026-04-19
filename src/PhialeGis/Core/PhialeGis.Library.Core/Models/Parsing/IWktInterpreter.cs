using PhialeGis.Library.Core.Models.Geometry.Interfaces;

namespace PhialeGis.Library.Core.Models.Parsing
{
    internal interface IWKTInterpreter
    {
        // Creates a new IGeometry instance based on the WKT string
        IGeometry CreateGeometryFromWKT(string wkt);

        // Loads data into an existing IGeometry instance from the WKT string
        void LoadGeometryFromWKT(IGeometry geometry, string wkt);
    }
}