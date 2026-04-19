using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.PdfViewer
{
    public sealed class PdfViewerRuntime : IDisposable
    {
        private readonly IWebComponentHost _host;
        private readonly PdfViewerWorkspace _workspace;
        private readonly PdfViewerOptions _options;

        private Task _initializeTask;
        private bool _disposed;
        private int _pageCount;
        private int _currentPage;
        private double _currentScaleFactor = 1d;
        private string _currentScaleValue = "page-width";

        public PdfViewerRuntime(
            IWebComponentHost host,
            PdfViewerWorkspace workspace,
            PdfViewerOptions options)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _options = (options ?? new PdfViewerOptions()).Clone();

            _host.ReadyStateChanged += HandleHostReadyStateChanged;
            _host.MessageReceived += HandleHostMessageReceived;
        }

        public bool IsInitialized => _host.IsInitialized;

        public bool IsReady => _host.IsReady;

        public int PageCount => _pageCount;

        public int CurrentPage => _currentPage;

        public double CurrentScaleFactor => _currentScaleFactor;

        public string CurrentScaleValue => _currentScaleValue;

        public event EventHandler<PdfViewerReadyStateChangedEventArgs> ReadyStateChanged;

        public event EventHandler<PdfViewerDocumentLoadedEventArgs> DocumentLoaded;

        public event EventHandler<PdfViewerPageChangedEventArgs> PageChanged;

        public event EventHandler<PdfViewerZoomChangedEventArgs> ZoomChanged;

        public event EventHandler<PdfViewerErrorEventArgs> ErrorOccurred;

        public Task InitializeAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PdfViewerRuntime));

            if (_initializeTask == null)
                _initializeTask = InitializeCoreAsync();

            return _initializeTask;
        }

        public async Task OpenAsync(PdfDocumentSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            await InitializeAsync().ConfigureAwait(false);
            var normalized = await _workspace.NormalizeAsync(source).ConfigureAwait(false);

            await _host.PostMessageAsync(new
            {
                type = "pdf.openSource",
                source = normalized.ViewerSource,
                displayName = normalized.DisplayName
            }).ConfigureAwait(false);
        }

        public async Task SetPageAsync(int pageNumber)
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.setPage", pageNumber }).ConfigureAwait(false);
        }

        public async Task SetZoomAsync(PdfZoomMode zoomMode)
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.setZoom", zoomMode = MapZoomMode(zoomMode) }).ConfigureAwait(false);
        }

        public async Task SetZoomAsync(double scaleFactor)
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.setZoom", scaleFactor }).ConfigureAwait(false);
        }

        public async Task SetSearchQueryAsync(string text)
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.setSearchQuery", query = text ?? string.Empty }).ConfigureAwait(false);
        }

        public async Task FindNextAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.findNext" }).ConfigureAwait(false);
        }

        public async Task FindPreviousAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.findPrevious" }).ConfigureAwait(false);
        }

        public async Task ClearSearchAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.clearSearch" }).ConfigureAwait(false);
        }

        public async Task PrintAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "pdf.print" }).ConfigureAwait(false);
        }

        public void FocusViewer()
        {
            _host.FocusHost();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _host.ReadyStateChanged -= HandleHostReadyStateChanged;
            _host.MessageReceived -= HandleHostMessageReceived;
            _host.Dispose();
            _workspace.Dispose();
        }

        private async Task InitializeCoreAsync()
        {
            await _workspace.PrepareAsync().ConfigureAwait(false);
            await _host.InitializeAsync().ConfigureAwait(false);
            await _host.LoadEntryPageAsync(_options.EntryPageRelativePath).ConfigureAwait(false);
        }

        private void HandleHostReadyStateChanged(object sender, WebComponentReadyStateChangedEventArgs e)
        {
            ReadyStateChanged?.Invoke(this, new PdfViewerReadyStateChangedEventArgs(e.IsInitialized, e.IsReady));
        }

        private void HandleHostMessageReceived(object sender, WebComponentMessageEventArgs e)
        {
            if (_disposed || string.IsNullOrWhiteSpace(e.RawMessage))
                return;

            try
            {
                using (var document = JsonDocument.Parse(e.RawMessage))
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                        return;

                    string messageType = ReadString(document.RootElement, "type");
                    switch (messageType)
                    {
                        case "pdf.documentLoaded":
                            _pageCount = ReadInt(document.RootElement, "pageCount");
                            _currentPage = ReadInt(document.RootElement, "currentPage");
                            DocumentLoaded?.Invoke(
                                this,
                                new PdfViewerDocumentLoadedEventArgs(
                                    _pageCount,
                                    _currentPage,
                                    ReadString(document.RootElement, "source"),
                                    ReadString(document.RootElement, "displayName")));
                            PageChanged?.Invoke(this, new PdfViewerPageChangedEventArgs(_currentPage, _pageCount));
                            break;

                        case "pdf.pageChanged":
                            _currentPage = ReadInt(document.RootElement, "pageNumber");
                            _pageCount = Math.Max(_pageCount, ReadInt(document.RootElement, "pageCount"));
                            PageChanged?.Invoke(this, new PdfViewerPageChangedEventArgs(_currentPage, _pageCount));
                            break;

                        case "pdf.zoomChanged":
                            _currentScaleFactor = ReadDouble(document.RootElement, "scaleFactor", _currentScaleFactor);
                            _currentScaleValue = ReadString(document.RootElement, "scaleValue");
                            ZoomChanged?.Invoke(this, new PdfViewerZoomChangedEventArgs(_currentScaleFactor, _currentScaleValue));
                            break;

                        case "pdf.error":
                            ErrorOccurred?.Invoke(
                                this,
                                new PdfViewerErrorEventArgs(
                                    ReadString(document.RootElement, "message"),
                                    ReadString(document.RootElement, "detail")));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new PdfViewerErrorEventArgs("Failed to parse PDF viewer message.", ex.Message));
            }
        }

        private static string MapZoomMode(PdfZoomMode zoomMode)
        {
            switch (zoomMode)
            {
                case PdfZoomMode.ActualSize:
                    return "page-actual";
                case PdfZoomMode.FitWidth:
                    return "page-width";
                case PdfZoomMode.FitPage:
                    return "page-fit";
                default:
                    return "auto";
            }
        }

        private static string ReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return string.Empty;

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static int ReadInt(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return 0;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                return intValue;

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static double ReadDouble(JsonElement root, string propertyName, double fallback)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return fallback;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var doubleValue))
                return doubleValue;

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}
