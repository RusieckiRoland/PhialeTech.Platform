namespace PhialeGis.Library.Abstractions.Interactions.Input
{
    public sealed class CoreManipulationInput
    {
        public CoreDeviceType DeviceType { get; set; }
        public CorePoint Position { get; set; }
        public CorePoint PivotCenter { get; set; }
        public bool HasPivotCenter { get; set; }
        public double Scale { get; set; }
    }
}
