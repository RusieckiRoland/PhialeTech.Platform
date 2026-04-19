namespace PhialeGis.Library.Abstractions.Interactions.Input
{
    public sealed class CorePointerInput
    {
        public uint PointerId { get; set; }
        public CoreDeviceType DeviceType { get; set; }
        public CorePoint Position { get; set; }
    }
}
