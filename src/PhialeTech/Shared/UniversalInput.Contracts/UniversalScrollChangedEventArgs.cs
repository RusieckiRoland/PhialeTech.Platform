namespace UniversalInput.Contracts
{
    /// <summary>
    /// Neutral host signal describing a viewport scroll offset change.
    /// This is not a raw pointer input. It represents the host's scroll state.
    /// </summary>
    public sealed class UniversalScrollChangedEventArgs : IUniversalBase
    {
        public UniversalScrollChangedEventArgs(double horizontalOffset, double verticalOffset)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
        }

        public double HorizontalOffset { get; }

        public double VerticalOffset { get; }

        public DeviceType PointerDeviceType { get; } = DeviceType.Other;

        public UniversalMetadata Metadata { get; set; }
    }
}
