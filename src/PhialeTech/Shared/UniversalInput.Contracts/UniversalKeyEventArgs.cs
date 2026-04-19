namespace UniversalInput.Contracts
{
    /// <summary>
    /// Universal keyboard event contract for platform adapters.
    /// </summary>
    public sealed class UniversalKeyEventArgs : IUniversalBase
    {
        public UniversalKeyEventArgs(string key, bool isKeyDown)
        {
            Key = key ?? string.Empty;
            IsKeyDown = isKeyDown;
        }

        public string Key { get; }

        public bool IsKeyDown { get; }

        public bool IsRepeat { get; set; }

        public bool Handled { get; set; }

        public DeviceType PointerDeviceType => DeviceType.Other;

        public UniversalMetadata Metadata { get; set; }
    }
}
