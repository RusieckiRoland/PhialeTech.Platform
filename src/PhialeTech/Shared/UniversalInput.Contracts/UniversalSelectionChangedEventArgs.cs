namespace UniversalInput.Contracts
{
    /// <summary>Raised when selection changes.</summary>
    public sealed class UniversalSelectionChangedEventArgs : IUniversalBase
    {
        /// <summary>Zero-based absolute start offset.</summary>
        public int Start { get; set; }
        /// <summary>Zero-based absolute end offset (exclusive).</summary>
        public int End { get; set; }
        /// <summary>Convenience flag.</summary>
        public bool IsEmpty { get; set; }

        /// <summary>Active caret location (optional convenience).</summary>
        public int CaretLine { get; set; }
        public int CaretColumn { get; set; }

        public DeviceType PointerDeviceType { get; private set; } = DeviceType.Other;
        public UniversalMetadata Metadata { get; set; }

        public UniversalSelectionChangedEventArgs(int start, int end, int caretLine, int caretColumn)
        {
            Start = start;
            End = end;
            IsEmpty = start == end;
            CaretLine = caretLine;
            CaretColumn = caretColumn;
        }
    }
}
