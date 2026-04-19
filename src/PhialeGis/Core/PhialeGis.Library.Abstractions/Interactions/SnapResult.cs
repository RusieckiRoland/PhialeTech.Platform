namespace PhialeGis.Library.Abstractions.Interactions
{
    public sealed class SnapResult
    {
        public bool HasSnap { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public SnapKind Kind { get; set; }

        public string LayerId { get; set; }

        public long? FeatureId { get; set; }

        public double DistanceModel { get; set; }
    }
}
