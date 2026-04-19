using System;

namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfViewerDocumentLoadedEventArgs : EventArgs
    {
        public PdfViewerDocumentLoadedEventArgs(int pageCount, int currentPage, string source, string displayName)
        {
            PageCount = pageCount;
            CurrentPage = currentPage;
            Source = source ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public int PageCount { get; }

        public int CurrentPage { get; }

        public string Source { get; }

        public string DisplayName { get; }
    }
}
