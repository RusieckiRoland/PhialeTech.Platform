namespace UniversalInput.Contracts
{
    /// <summary>Raised when language mode changes (e.g., "csharp", "json").</summary>
    public sealed class UniversalLanguageChangedEventArgs : IUniversalBase
    {
        public string LanguageId { get; set; } = "plaintext";

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalLanguageChangedEventArgs(string languageId) => LanguageId = languageId ?? "plaintext";
    }
}
