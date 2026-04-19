using System.IO;

namespace PhialeGis.Library.Core.Models.Geometry.Interfaces
{
    internal interface IGeometry
    {
        PhRect GetBoundingBox();

        void LoadFromStream(Stream stream);
    }
}