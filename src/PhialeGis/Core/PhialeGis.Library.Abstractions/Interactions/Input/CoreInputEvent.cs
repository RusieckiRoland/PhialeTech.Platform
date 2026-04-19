namespace PhialeGis.Library.Abstractions.Interactions.Input
{
    public sealed class CoreInputEvent
    {
        public CoreInputKind Kind { get; set; }
        public CoreDeviceType DeviceType { get; set; }
        public CorePointerInput Pointer { get; set; }
        public CoreManipulationInput Manipulation { get; set; }
        public bool ResetManipulationMode { get; set; }
    }
}
