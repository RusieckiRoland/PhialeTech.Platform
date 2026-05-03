namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorOptions : IDocumentEditorOverlayOptions
    {
        public string EntryPageRelativePath { get; set; } = "DocumentEditor/index.html";

        public string ReadyMessageType { get; set; } = "documentEditor.ready";

        public string VirtualHostName { get; set; } = "phialetech.documenteditor";

        public string AssetRootRelativePath { get; set; } = "DocumentEditor";

        public bool IsReadOnly { get; set; }

        public string InitialTheme { get; set; } = "light";

        public string InitialLanguageCode { get; set; } = "en";

        public string Placeholder { get; set; } = string.Empty;

        public string InitialDocumentJson { get; set; } = string.Empty;

        public DocumentEditorOverlayMode OverlayMode { get; set; } = DocumentEditorOverlayMode.Window;

        DocumentEditorOverlayMode? IDocumentEditorOverlayOptions.OverlayMode
        {
            get { return OverlayMode; }
        }

        public DocumentEditorToolbarConfig Toolbar { get; set; } = DocumentEditorToolbarConfig.CreateDefault();

        public DocumentEditorOptions Clone()
        {
            return new DocumentEditorOptions
            {
                EntryPageRelativePath = EntryPageRelativePath,
                ReadyMessageType = ReadyMessageType,
                VirtualHostName = VirtualHostName,
                AssetRootRelativePath = AssetRootRelativePath,
                IsReadOnly = IsReadOnly,
                InitialTheme = string.IsNullOrWhiteSpace(InitialTheme) ? "light" : InitialTheme,
                InitialLanguageCode = string.IsNullOrWhiteSpace(InitialLanguageCode) ? "en" : InitialLanguageCode,
                Placeholder = Placeholder ?? string.Empty,
                InitialDocumentJson = InitialDocumentJson ?? string.Empty,
                OverlayMode = OverlayMode,
                Toolbar = (Toolbar ?? DocumentEditorToolbarConfig.CreateDefault()).Clone(),
            };
        }
    }
}
