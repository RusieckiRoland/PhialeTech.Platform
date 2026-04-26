namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorToolbarItem
    {
        public DocumentEditorCommand Command { get; set; }

        public bool IsVisible { get; set; } = true;

        public int Order { get; set; }
    }
}
