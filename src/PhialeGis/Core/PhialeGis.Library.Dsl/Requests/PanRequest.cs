namespace PhialeGis.Library.Dsl.Requests
{
    /// <summary>
    /// Represents a pan operation from a source point to a destination point.
    /// The runtime may apply this as a translation vector (To - From).
    /// </summary>
    public sealed class PanRequest
    {
        public double FromX { get; set; }
        public double FromY { get; set; }
        public double ToX { get; set; }
        public double ToY { get; set; }
    }
}
