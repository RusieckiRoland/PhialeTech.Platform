namespace UniversalInput.Contracts
{
    /// <summary>Fired when user/API triggers a save.</summary>
    public sealed class UniversalSaveRequestedEventArgs : IUniversalBase
    {
        /// <summary>Reason hint (e.g., "shortcut", "menu", "autoSave").</summary>
        public string Reason { get; set; } = string.Empty;

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalSaveRequestedEventArgs(string reason) => Reason = reason ?? string.Empty;
    }
}
