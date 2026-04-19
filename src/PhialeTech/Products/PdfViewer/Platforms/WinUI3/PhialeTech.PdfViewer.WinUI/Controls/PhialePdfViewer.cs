using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.WinUI;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.System;

namespace PhialeTech.PdfViewer.WinUI.Controls
{
    public sealed class PhialePdfViewer : UserControl, IPdfViewer
    {
        private readonly PdfViewerOptions _options;
        private readonly PdfViewerWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly PdfViewerRuntime _runtime;
        private readonly TextBox _pageTextBox;
        private readonly TextBlock _pageCountText;
        private readonly TextBlock _zoomText;
        private readonly TextBox _searchTextBox;
        private readonly TextBlock _statusText;
        private readonly Button _previousPageButton;
        private readonly Button _nextPageButton;
        private readonly Button _zoomOutButton;
        private readonly Button _zoomInButton;
        private readonly Button _fitWidthButton;
        private readonly Button _fitPageButton;
        private readonly Button _findPreviousButton;
        private readonly Button _findNextButton;
        private readonly Button _clearSearchButton;
        private readonly Button _printButton;
        private readonly Button _focusButton;
        private bool _disposed;
        private int _pageCount;
        private int _currentPage;
        private double _currentScaleFactor = 1d;
        private string _currentScaleValue = "page-width";

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

            var hostElement = _host as UIElement;
            if (hostElement is null)
                throw new InvalidOperationException("The supplied WinUI web host factory did not return a WinUI UI element.");

            _runtime = new PdfViewerRuntime(_host, _workspace, _options);
            _runtime.ReadyStateChanged += HandleReadyStateChanged;
            _runtime.DocumentLoaded += HandleDocumentLoaded;
            _runtime.PageChanged += HandlePageChanged;
            _runtime.ZoomChanged += HandleZoomChanged;
            _runtime.ErrorOccurred += HandleErrorOccurred;

            _previousPageButton = CreateButton("Prev");
            _previousPageButton.Click += async (_, __) => await RunUiActionAsync(() => SetPageAsync(_currentPage - 1), "Failed to move to the previous page.");

            _nextPageButton = CreateButton("Next");
            _nextPageButton.Click += async (_, __) => await RunUiActionAsync(() => SetPageAsync(_currentPage + 1), "Failed to move to the next page.");

            _pageTextBox = new TextBox
            {
                Width = 56,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _pageTextBox.KeyDown += HandlePageTextBoxKeyDown;

            _pageCountText = new TextBlock
            {
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.SlateGray),
                Text = "/ 0"
            };

            _zoomOutButton = CreateButton("-");
            _zoomOutButton.Click += async (_, __) => await RunUiActionAsync(() => StepZoomAsync(0.85d), "Failed to decrease zoom.");

            _zoomInButton = CreateButton("+");
            _zoomInButton.Click += async (_, __) => await RunUiActionAsync(() => StepZoomAsync(1.15d), "Failed to increase zoom.");

            _fitWidthButton = CreateButton("Fit width");
            _fitWidthButton.Click += async (_, __) => await RunUiActionAsync(() => SetZoomAsync(PdfZoomMode.FitWidth), "Failed to apply fit width.");

            _fitPageButton = CreateButton("Fit page");
            _fitPageButton.Click += async (_, __) => await RunUiActionAsync(() => SetZoomAsync(PdfZoomMode.FitPage), "Failed to apply fit page.");

            _zoomText = new TextBlock
            {
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.SteelBlue),
                Text = "100%"
            };

            _searchTextBox = new TextBox
            {
                Width = 180,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _searchTextBox.KeyDown += HandleSearchTextBoxKeyDown;

            _findPreviousButton = CreateButton("Find prev");
            _findPreviousButton.Click += async (_, __) => await RunUiActionAsync(FindPreviousFromUiAsync, "Failed to find the previous match.");

            _findNextButton = CreateButton("Find next");
            _findNextButton.Click += async (_, __) => await RunUiActionAsync(FindNextFromUiAsync, "Failed to find the next match.");

            _clearSearchButton = CreateButton("Clear");
            _clearSearchButton.Click += async (_, __) => await RunUiActionAsync(ClearSearchFromUiAsync, "Failed to clear the search.");

            _printButton = CreateButton("Print");
            _printButton.Click += async (_, __) => await RunUiActionAsync(PrintAsync, "Failed to start print flow.");

            _focusButton = CreateButton("Focus");
            _focusButton.Click += (_, __) => FocusViewer();

            _statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.SlateGray),
                Text = "Status: waiting for browser host"
            };

            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            toolbarPanel.Children.Add(_previousPageButton);
            toolbarPanel.Children.Add(_nextPageButton);
            toolbarPanel.Children.Add(_pageTextBox);
            toolbarPanel.Children.Add(_pageCountText);
            toolbarPanel.Children.Add(_zoomOutButton);
            toolbarPanel.Children.Add(_zoomInButton);
            toolbarPanel.Children.Add(_fitWidthButton);
            toolbarPanel.Children.Add(_fitPageButton);
            toolbarPanel.Children.Add(_zoomText);
            toolbarPanel.Children.Add(_searchTextBox);
            toolbarPanel.Children.Add(_findPreviousButton);
            toolbarPanel.Children.Add(_findNextButton);
            toolbarPanel.Children.Add(_clearSearchButton);
            toolbarPanel.Children.Add(_printButton);
            toolbarPanel.Children.Add(_focusButton);

            var toolbarScroller = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Enabled,
                VerticalScrollMode = ScrollMode.Disabled,
                Content = toolbarPanel
            };

            var hostBorder = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Colors.WhiteSmoke),
                Child = hostElement
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(toolbarScroller, 0);
            Grid.SetRow(_statusText, 1);
            Grid.SetRow(hostBorder, 2);
            root.Children.Add(toolbarScroller);
            root.Children.Add(_statusText);
            root.Children.Add(hostBorder);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            UpdateUiState();
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
            _pageTextBox.KeyDown -= HandlePageTextBoxKeyDown;
            _searchTextBox.KeyDown -= HandleSearchTextBoxKeyDown;
            _runtime.ReadyStateChanged -= HandleReadyStateChanged;
            _runtime.DocumentLoaded -= HandleDocumentLoaded;
            _runtime.PageChanged -= HandlePageChanged;
            _runtime.ZoomChanged -= HandleZoomChanged;
            _runtime.ErrorOccurred -= HandleErrorOccurred;
            _runtime.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            _ = RunUiActionAsync(InitializeAsync, "Failed to initialize the PDF viewer.");
        }

        private void HandleReadyStateChanged(object? sender, PdfViewerReadyStateChangedEventArgs e)
        {
            SetStatusText(e.IsReady
                ? "Status: viewer ready"
                : e.IsInitialized
                    ? "Status: browser host initialized"
                    : "Status: waiting for browser host");
            UpdateUiState();
        }

        private void HandleDocumentLoaded(object? sender, PdfViewerDocumentLoadedEventArgs e)
        {
            _pageCount = e.PageCount;
            _currentPage = e.CurrentPage;
            _pageTextBox.Text = _currentPage.ToString(CultureInfo.InvariantCulture);
            _pageCountText.Text = "/ " + _pageCount.ToString(CultureInfo.InvariantCulture);
            string loadedName = string.IsNullOrWhiteSpace(e.DisplayName) ? e.Source : e.DisplayName;
            SetStatusText(string.IsNullOrWhiteSpace(loadedName)
                ? "Status: PDF document loaded"
                : "Status: loaded " + loadedName);
            UpdateUiState();
        }

        private void HandlePageChanged(object? sender, PdfViewerPageChangedEventArgs e)
        {
            _currentPage = e.PageNumber;
            _pageCount = e.PageCount;
            _pageTextBox.Text = _currentPage.ToString(CultureInfo.InvariantCulture);
            _pageCountText.Text = "/ " + _pageCount.ToString(CultureInfo.InvariantCulture);
            UpdateUiState();
        }

        private void HandleZoomChanged(object? sender, PdfViewerZoomChangedEventArgs e)
        {
            _currentScaleFactor = e.ScaleFactor;
            _currentScaleValue = e.ScaleValue;
            _zoomText.Text = FormatZoomText(e.ScaleFactor, e.ScaleValue);
        }

        private void HandleErrorOccurred(object? sender, PdfViewerErrorEventArgs e)
        {
            string message = string.IsNullOrWhiteSpace(e.Message) ? "PDF viewer error" : e.Message;
            if (!string.IsNullOrWhiteSpace(e.Detail))
                message += " " + e.Detail;

            SetStatusText("Status: " + message);
        }

        private async Task StepZoomAsync(double factor)
        {
            double next = Math.Max(0.25d, Math.Min(5d, _currentScaleFactor * factor));
            await SetZoomAsync(next).ConfigureAwait(true);
        }

        private async Task FindNextFromUiAsync()
        {
            await SetSearchQueryAsync(_searchTextBox.Text ?? string.Empty).ConfigureAwait(true);
            await FindNextAsync().ConfigureAwait(true);
        }

        private async Task FindPreviousFromUiAsync()
        {
            await SetSearchQueryAsync(_searchTextBox.Text ?? string.Empty).ConfigureAwait(true);
            await FindPreviousAsync().ConfigureAwait(true);
        }

        private async Task ClearSearchFromUiAsync()
        {
            _searchTextBox.Text = string.Empty;
            await ClearSearchAsync().ConfigureAwait(true);
        }

        private async Task RunUiActionAsync(Func<Task> action, string errorPrefix)
        {
            if (_disposed)
                return;

            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                SetStatusText(errorPrefix + " " + ex.Message);
            }
        }

        private void HandlePageTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;

            e.Handled = true;

            if (!int.TryParse(_pageTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageNumber))
            {
                _pageTextBox.Text = _currentPage.ToString(CultureInfo.InvariantCulture);
                return;
            }

            _ = RunUiActionAsync(() => SetPageAsync(pageNumber), "Failed to navigate to the requested page.");
        }

        private void HandleSearchTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;

            e.Handled = true;
            _ = RunUiActionAsync(FindNextFromUiAsync, "Failed to find the next match.");
        }

        private void UpdateUiState()
        {
            bool hasDocument = _pageCount > 0;
            bool canInteract = IsReady && hasDocument;

            _previousPageButton.IsEnabled = canInteract && _currentPage > 1;
            _nextPageButton.IsEnabled = canInteract && _currentPage < _pageCount;
            _pageTextBox.IsEnabled = canInteract;
            _zoomOutButton.IsEnabled = canInteract;
            _zoomInButton.IsEnabled = canInteract;
            _fitWidthButton.IsEnabled = canInteract;
            _fitPageButton.IsEnabled = canInteract;
            _searchTextBox.IsEnabled = canInteract;
            _findPreviousButton.IsEnabled = canInteract;
            _findNextButton.IsEnabled = canInteract;
            _clearSearchButton.IsEnabled = canInteract;
            _printButton.IsEnabled = canInteract;
            _focusButton.IsEnabled = IsInitialized;

            if (string.IsNullOrWhiteSpace(_pageTextBox.Text))
                _pageTextBox.Text = hasDocument ? _currentPage.ToString(CultureInfo.InvariantCulture) : "1";

            if (string.IsNullOrWhiteSpace(_zoomText.Text))
                _zoomText.Text = FormatZoomText(_currentScaleFactor, _currentScaleValue);
        }

        private void SetStatusText(string text)
        {
            _statusText.Text = text ?? string.Empty;
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                Margin = new Thickness(0, 0, 6, 6),
                Padding = new Thickness(10, 6, 10, 6),
                MinWidth = 34,
                Content = text
            };
        }

        private static string FormatZoomText(double scaleFactor, string scaleValue)
        {
            switch ((scaleValue ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "page-width":
                    return "Fit width";
                case "page-fit":
                    return "Fit page";
                case "page-actual":
                    return "100%";
                case "auto":
                    return "Auto";
                default:
                    if (scaleFactor > 0)
                        return Math.Round(scaleFactor * 100d).ToString("0", CultureInfo.InvariantCulture) + "%";

                    return "100%";
            }
        }
    }
}
