using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Neutralny input opisujący zmianę wartości aktywnego edytora komórki.
    /// Host platformowy nie interpretuje semantyki zmiany - tylko przekazuje ją do Core.
    /// </summary>
    public sealed class GridEditorValueInput : GridInputEvent
    {
        public GridEditorValueInput(
            DateTime timestamp,
            string rowKey,
            string columnKey,
            string value,
            GridEditorValueChangeKind changeKind,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            RowKey = rowKey ?? string.Empty;
            ColumnKey = columnKey ?? string.Empty;
            Value = value ?? string.Empty;
            ChangeKind = changeKind;
        }

        public string RowKey { get; }

        public string ColumnKey { get; }

        public string Value { get; }

        public GridEditorValueChangeKind ChangeKind { get; }
    }

    public enum GridEditorValueChangeKind
    {
        TextEdited,
        SelectionCommitted,
    }
}
