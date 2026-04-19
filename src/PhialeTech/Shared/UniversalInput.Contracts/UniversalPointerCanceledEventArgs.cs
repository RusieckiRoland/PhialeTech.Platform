namespace UniversalInput.Contracts
{
    /// <summary>
    /// Shared pointer-canceled contract emitted by platform adapters when an active pointer interaction
    /// ends without a normal release (for example capture loss or focus loss).
    /// </summary>
    public sealed class UniversalPointerCanceledEventArgs : IUniversalBase
    {
        public UniversalPointerCanceledEventArgs(UniversalPointer pointer, UniversalPointerCancelReason reason)
        {
            Pointer = pointer;
            Reason = reason;
        }

        public UniversalPointer Pointer { get; }

        public UniversalPointerCancelReason Reason { get; }

        public DeviceType PointerDeviceType => Pointer.PointerDeviceType;

        public UniversalMetadata Metadata { get; set; }
    }

    public enum UniversalPointerCancelReason
    {
        CaptureLost,
        FocusLost,
        Unloaded,
        ManipulationStarted,
        PlatformCanceled,
    }
}
