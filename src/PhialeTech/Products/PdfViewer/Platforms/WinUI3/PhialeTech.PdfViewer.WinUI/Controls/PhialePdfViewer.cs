using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.WinUI;
using System;
using System.Threading.Tasks;

namespace PhialeTech.PdfViewer.WinUI.Controls
{
    public sealed class PhialePdfViewer : UserControl, IPdfViewer
    {
        private readonly PdfViewerOptions _options;
        private readonly PdfViewerWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly PdfViewerRuntime _runtime;
        private bool _disposed;

        public PhialePdfViewer()
            : this(new WinUiWebComponentHostFactory(), new PdfViewerOptions())
        {
        }

        public PhialePdfViewer(IWebComponentHostFactory hostFactory, PdfViewerOptions? options = null)
        {
            if (hostFactory is null)
                throw new ArgumentNullException(nameof(hostFactory));

            _options = (options ?? new PdfViewerOptions()).Clone();
            _workspace = new PdfViewerWorkspace(_options);
            _host = hostFactory.CreateHost(new WebComponentHostOptions
            {
                LocalContentRootPath = _workspace.WorkspaceRootPath,
                JavaScriptReadyMessageType = _options.ReadyMessageType,
                VirtualHostName = _options.VirtualHostName,
                QueueMessagesUntilReady = true
            });

            if (_host is not UIElement hostElement)
                throw new InvalidOperationException("The supplied WinUI web host factory did not return a WinUI UI element.");

            _runtime = new PdfViewerRuntime(_host, _workspace, _options);

            if (hostElement is FrameworkElement frameworkElement)
            {
                frameworkElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                frameworkElement.VerticalAlignment = VerticalAlignment.Stretch;
                frameworkElement.MinWidth = 0d;
                frameworkElement.MinHeight = 0d;
            }

            var hostSurface = new Border
            {
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(14),
                Child = hostElement
            };

            Content = hostSurface;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
        }

        public PdfViewerOptions Options => _options;

        public bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public event EventHandler<PdfViewerReadyStateChangedEventArgs> ReadyStateChanged
        {
            add => _runtime.ReadyStateChanged += value;
            remove => _runtime.ReadyStateChanged -= value;
        }

        public event EventHandler<PdfViewerDocumentLoadedEventArgs> DocumentLoaded
        {
            add => _runtime.DocumentLoaded += value;
            remove => _runtime.DocumentLoaded -= value;
        }

        public event EventHandler<PdfViewerPageChangedEventArgs> PageChanged
        {
            add => _runtime.PageChanged += value;
            remove => _runtime.PageChanged -= value;
        }

        public event EventHandler<PdfViewerZoomChangedEventArgs> ZoomChanged
        {
            add => _runtime.ZoomChanged += value;
            remove => _runtime.ZoomChanged -= value;
        }

        public event EventHandler<PdfViewerErrorEventArgs> ErrorOccurred
        {
            add => _runtime.ErrorOccurred += value;
            remove => _runtime.ErrorOccurred -= value;
        }

        public Task InitializeAsync() => _runtime.InitializeAsync();

        public Task OpenAsync(PdfDocumentSource source) => _runtime.OpenAsync(source);

        public Task SetPageAsync(int pageNumber) => _runtime.SetPageAsync(pageNumber);

        public Task SetZoomAsync(PdfZoomMode zoomMode) => _runtime.SetZoomAsync(zoomMode);

        public Task SetZoomAsync(double scaleFactor) => _runtime.SetZoomAsync(scaleFactor);

        public Task SetSearchQueryAsync(string text) => _runtime.SetSearchQueryAsync(text);

        public Task FindNextAsync() => _runtime.FindNextAsync();

        public Task FindPreviousAsync() => _runtime.FindPreviousAsync();

        public Task ClearSearchAsync() => _runtime.ClearSearchAsync();

        public Task PrintAsync() => _runtime.PrintAsync();

        public Task SetThemeAsync(string theme) => _runtime.SetThemeAsync(theme);

        public void FocusViewer()
        {
            if (_disposed)
                return;

            _runtime.FocusViewer();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Loaded -= HandleLoaded;
            _runtime.Dispose();
        }

        private async void HandleLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await InitializeAsync().ConfigureAwait(true);
            }
            catch
            {
                // Surface-level host; runtime errors are reported via ErrorOccurred.
            }
        }
    }
}
