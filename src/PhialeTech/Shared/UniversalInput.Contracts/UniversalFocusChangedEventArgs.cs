namespace UniversalInput.Contracts
{
    /// <summary>
    /// Universal focus changed event contract for platform adapters.
    /// </summary>
    public sealed class UniversalFocusChangedEventArgs : IUniversalBase
    {
        public UniversalFocusChangedEventArgs(bool hasFocus)
        {
            HasFocus = hasFocus;
        }

        public bool HasFocus { get; }

        public DeviceType PointerDeviceType => DeviceType.Other;

        public UniversalMetadata Metadata { get; set; }
    }
}
