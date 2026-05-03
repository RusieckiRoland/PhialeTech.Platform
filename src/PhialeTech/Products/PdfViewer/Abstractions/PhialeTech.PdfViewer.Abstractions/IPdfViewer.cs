using System;
using System.Threading.Tasks;

namespace PhialeTech.PdfViewer.Abstractions
{
    public interface IPdfViewer : IDisposable
    {
        PdfViewerOptions Options { get; }

        bool IsInitialized { get; }

        bool IsReady { get; }

        event EventHandler<PdfViewerReadyStateChangedEventArgs> ReadyStateChanged;

        event EventHandler<PdfViewerDocumentLoadedEventArgs> DocumentLoaded;

        event EventHandler<PdfViewerPageChangedEventArgs> PageChanged;

        event EventHandler<PdfViewerZoomChangedEventArgs> ZoomChanged;

        event EventHandler<PdfViewerErrorEventArgs> ErrorOccurred;

        Task InitializeAsync();

        Task OpenAsync(PdfDocumentSource source);

        Task SetPageAsync(int pageNumber);

        Task SetZoomAsync(PdfZoomMode zoomMode);

        Task SetZoomAsync(double scaleFactor);

        Task SetSearchQueryAsync(string text);

        Task FindNextAsync();

        Task FindPreviousAsync();

        Task ClearSearchAsync();

        Task PrintAsync();

        Task SetThemeAsync(string theme);

        void FocusViewer();
    }
}
