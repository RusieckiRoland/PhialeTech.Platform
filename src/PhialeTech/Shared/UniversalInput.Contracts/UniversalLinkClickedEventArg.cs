namespace UniversalInput.Contracts
{
    /// <summary>Raised when user clicks a link/URI inside editor.</summary>
    public sealed class UniversalLinkClickedEventArgs : IUniversalBase
    {
        public string Url { get; set; } = string.Empty;

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalLinkClickedEventArgs(string url) => Url = url ?? string.Empty;
    }
}
