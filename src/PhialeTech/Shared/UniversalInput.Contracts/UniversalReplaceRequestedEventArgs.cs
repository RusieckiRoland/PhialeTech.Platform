namespace UniversalInput.Contracts
{
    /// <summary>Raised when Replace is requested.</summary>
    public sealed class UniversalReplaceRequestedEventArgs : IUniversalBase
    {
        public string Query { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public bool MatchCase { get; set; }
        public bool Regex { get; set; }
        public bool WholeWord { get; set; }

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalReplaceRequestedEventArgs(string query, string replacement, bool matchCase, bool regex, bool wholeWord)
        {
            Query = query ?? string.Empty;
            Replacement = replacement ?? string.Empty;
            MatchCase = matchCase; Regex = regex; WholeWord = wholeWord;
        }
    }
}
