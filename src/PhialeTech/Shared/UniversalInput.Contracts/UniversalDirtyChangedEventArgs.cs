namespace UniversalInput.Contracts
{
    /// <summary>Raised when "dirty" state (unsaved changes) toggles.</summary>
    public sealed class UniversalDirtyChangedEventArgs : IUniversalBase
    {
        public bool IsDirty { get; set; }

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalDirtyChangedEventArgs(bool isDirty) => IsDirty = isDirty;
    }
}
