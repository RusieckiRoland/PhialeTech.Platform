namespace UniversalInput.Contracts
{
    /// <summary>Raised when user opens/uses Find.</summary>
    public sealed class UniversalFindRequestedEventArgs : IUniversalBase
    {
        public string Query { get; set; } = string.Empty;
        public bool MatchCase { get; set; }
        public bool Regex { get; set; }
        public bool WholeWord { get; set; }

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalFindRequestedEventArgs(string query, bool matchCase, bool regex, bool wholeWord)
        {
            Query = query ?? string.Empty;
            MatchCase = matchCase; Regex = regex; WholeWord = wholeWord;
        }
    }
}
