using System;

namespace PhialeTech.PdfViewer.Abstractions
{
    public sealed class PdfViewerReadyStateChangedEventArgs : EventArgs
    {
        public PdfViewerReadyStateChangedEventArgs(bool isInitialized, bool isReady)
        {
            IsInitialized = isInitialized;
            IsReady = isReady;
        }

        public bool IsInitialized { get; }

        public bool IsReady { get; }
    }
}
