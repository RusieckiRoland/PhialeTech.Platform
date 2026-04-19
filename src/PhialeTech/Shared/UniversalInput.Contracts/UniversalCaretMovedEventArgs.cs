namespace UniversalInput.Contracts
{
    /// <summary>Raised when caret (cursor) moves.</summary>
    public sealed class UniversalCaretMovedEventArgs : IUniversalBase
    {
        /// <summary>1-based line index.</summary>
        public int Line { get; set; }
        /// <summary>1-based column index.</summary>
        public int Column { get; set; }
        /// <summary>Zero-based absolute offset.</summary>
        public int Offset { get; set; }

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalCaretMovedEventArgs(int line, int column, int offset)
        {
            Line = line;
            Column = column;
            Offset = offset;
        }
    }
}
