// PhialeGis.Library.Dsl/Requests/ImportLayerFromFileRequest.cs
namespace PhialeGis.Library.Dsl.Requests
{
    /// <summary>
    /// Explicit request to build a layer from a vector file (e.g., .fgb).
    /// Keeps semantics separate from AddLayer (which creates an empty/logical layer).
    /// </summary>
    public sealed class ImportLayerFromFileRequest
    {
        public string Path { get; set; }     // Absolute or app-relative file path
        public string Name { get; set; }     // Optional display name
        public bool? Visible { get; set; }   // Optional visibility (default: true)
        public double? Opacity { get; set; } // Optional opacity in [0..1] (default: 1.0)
        public string Crs { get; set; }      // Optional CRS hint (kept as metadata)
    }
}
