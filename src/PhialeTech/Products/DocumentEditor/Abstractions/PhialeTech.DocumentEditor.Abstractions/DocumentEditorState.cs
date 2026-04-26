namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorState
    {
        public bool IsReadOnly { get; set; }

        public bool CanUndo { get; set; }

        public bool CanRedo { get; set; }

        public bool IsDirty { get; set; }

        public bool IsEmpty { get; set; }
    }
}
