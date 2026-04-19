namespace UniversalInput.Contracts
{
    /// <summary>
    /// Neutralny kontrakt opisujący zmianę wartości aktywnego edytora komórki.
    /// </summary>
    public sealed class UniversalEditorValueChangedEventArgs : IUniversalBase
    {
        public UniversalEditorValueChangedEventArgs(
            string rowKey,
            string columnKey,
            string text,
            UniversalEditorValueChangeKind changeKind)
        {
            RowKey = rowKey ?? string.Empty;
            ColumnKey = columnKey ?? string.Empty;
            Text = text ?? string.Empty;
            ChangeKind = changeKind;
        }

        public string RowKey { get; }

        public string ColumnKey { get; }

        public string Text { get; }

        public UniversalEditorValueChangeKind ChangeKind { get; }

        public DeviceType PointerDeviceType => DeviceType.Other;

        public UniversalMetadata Metadata { get; set; }
    }
}
