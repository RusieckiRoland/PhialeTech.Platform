// PhialeGis.Library.Domain.IO.LayerFileLoader.cs
using System.IO;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.IO.FlatGeobuf;

namespace PhialeGis.Library.Domain.IO
{
    public static class LayerFileLoader
    {
        public static void TryLoadFromSource(PhLayer layer, string sourcePath)
        {
            if (layer == null || string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return;

            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext == ".fgb")
            {
                long id = 1;
                foreach (var f in PhFlatGeobufStore.Read(sourcePath)) // wymaga PH_FGB
                {
                    // Zapewnij ID (jeśli reader zwraca 0)
                    if (f.Id == 0) f.Id = id++;
                    layer.AddFeature(f);
                }
            }
        }
    }
}
