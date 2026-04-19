using System;

namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfViewerErrorEventArgs : EventArgs
    {
        public PdfViewerErrorEventArgs(string message, string detail)
        {
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Message { get; }

        public string Detail { get; }
    }
}
