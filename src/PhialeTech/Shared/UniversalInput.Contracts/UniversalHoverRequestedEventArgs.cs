namespace UniversalInput.Contracts
{
    /// <summary>Request to show hover/tooltip information at a given position.</summary>
    public sealed class UniversalHoverRequestedEventArgs : IUniversalBase
    {
        /// <summary>Zero-based absolute offset in document.</summary>
        public int Offset { get; set; }

        /// <summary>Optional line/column for convenience (1-based).</summary>
        public int Line { get; set; }
        public int Column { get; set; }

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalHoverRequestedEventArgs(int offset, int line, int column)
        {
            Offset = offset;
            Line = line;
            Column = column;
        }
    }
}
