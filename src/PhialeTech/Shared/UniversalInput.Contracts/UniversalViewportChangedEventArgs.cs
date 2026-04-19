namespace UniversalInput.Contracts
{
    /// <summary>
    /// Neutral host signal describing a viewport size change.
    /// This is not a raw pointer input. It represents the host's measured viewport.
    /// </summary>
    public sealed class UniversalViewportChangedEventArgs : IUniversalBase
    {
        public UniversalViewportChangedEventArgs(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }

        public double Height { get; }

        public DeviceType PointerDeviceType { get; } = DeviceType.Other;

        public UniversalMetadata Metadata { get; set; }
    }
}
