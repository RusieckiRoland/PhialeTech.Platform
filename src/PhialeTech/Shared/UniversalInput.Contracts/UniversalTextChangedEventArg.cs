namespace UniversalInput.Contracts
{
    /// <summary>Raised when editor content changes.</summary>
    public sealed class UniversalTextChangedEventArgs : IUniversalBase
    {
        public string Text { get; set; } = string.Empty;

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;

        public UniversalMetadata Metadata { get; set; }

        public UniversalTextChangedEventArgs(string text)
        {
            Text = text ?? string.Empty;
        }
    }
}
