namespace PhialeGis.Library.Core.Components
{
    /// <summary>Reference to a predefined line style by ID.</summary>
    public sealed class StrokeStyleRef
    {
        public string StyleId { get; set; }
        public StrokeStyleRef() { }
        public StrokeStyleRef(string styleId) { StyleId = styleId; }
    }
}
