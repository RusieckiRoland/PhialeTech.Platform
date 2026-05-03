namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfViewerOptions
    {
        public string EntryPageRelativePath { get; set; } = "PdfViewer/index.html";

        public string ReadyMessageType { get; set; } = "pdf.ready";

        public string VirtualHostName { get; set; } = "phialetech.pdfviewer";

        public string AssetRootRelativePath { get; set; } = "PdfViewer";

        public string InitialTheme { get; set; } = "light";

        public PdfViewerOptions Clone()
        {
            return new PdfViewerOptions
            {
                EntryPageRelativePath = EntryPageRelativePath,
                ReadyMessageType = ReadyMessageType,
                VirtualHostName = VirtualHostName,
                AssetRootRelativePath = AssetRootRelativePath,
                InitialTheme = InitialTheme
            };
        }
    }
}
