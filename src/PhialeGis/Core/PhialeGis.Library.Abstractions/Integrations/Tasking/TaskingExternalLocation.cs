namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public sealed class TaskingExternalLocation
    {
        public string LayerName { get; set; }
        public long? FeatureId { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public string GeometryWkt { get; set; }
    }
}
