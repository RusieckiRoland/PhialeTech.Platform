namespace UniversalInput.Contracts
{
    /// <summary>Raised when editor theme changes (e.g., "vs-dark").</summary>
    public sealed class UniversalThemeChangedEventArgs : IUniversalBase
    {
        public string ThemeId { get; set; } = "default";

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalThemeChangedEventArgs(string themeId) => ThemeId = themeId ?? "default";
    }
}
