namespace PhialeGis.Library.Dsl.Requests
{
    /// <summary>
    /// DTO for the ADDLAYER command with all supported options.
    /// Optional members are nullable so the runtime can apply canonical defaults.
    /// </summary>
    public sealed class AddLayerRequest
    {
        // Required by grammar
        public string Label { get; set; }   // Display name (STRING)
        public string Id { get; set; }   // Optional (AS Id); if null, generate from Label
        public string Type { get; set; }   // MEMORY | SPATIALITE | POSTGIS

        // Optional options
        public string Table { get; set; }   // Default: Id
        public string GeometryCol { get; set; }   // Default: "Geometry"
        public string GeometryType { get; set; }   // Default: "Geometry"
        public string Dim { get; set; }   // Default: "XY"
        public string Source { get; set; }   // Default: project SQLite URI
        public string Crs { get; set; }   // e.g., "EPSG:2180"

        public bool? Selectable { get; set; }   // Default: true
        public bool? Visible { get; set; }   // Default: true
        public double? Opacity { get; set; }   // Default: 1.0
    }
}
