using System;

namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfViewerPageChangedEventArgs : EventArgs
    {
        public PdfViewerPageChangedEventArgs(int pageNumber, int pageCount)
        {
            PageNumber = pageNumber;
            PageCount = pageCount;
        }

        public int PageNumber { get; }

        public int PageCount { get; }
    }
}
