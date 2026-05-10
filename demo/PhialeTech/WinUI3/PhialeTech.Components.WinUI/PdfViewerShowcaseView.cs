using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.PdfViewer.WinUI.Controls;
using PhialeTech.WebHost;
using PhialeTech.WebHost.WinUI;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace PhialeTech.Components.WinUI
{
    public sealed class PdfViewerShowcaseView : UserControl, IDisposable, IWebDemoFocusModeSource
    {
        private readonly Window _ownerWindow;
        private readonly PhialePdfViewer _viewer;
        private readonly Button _loadSampleButton;
        private readonly Button _openPdfButton;
        private readonly Button _expandButton;
        private readonly Grid _topBar;
        private readonly Border _viewerSurface;
        private readonly Grid _viewerOverlay;
        private readonly Grid _viewerPanel;
        private string _theme;
        private bool _opened;
        private bool _disposed;
        private bool _isFocusMode;

        bool IWebDemoFocusModeSource.IsFocusMode => _isFocusMode;
        bool IWebDemoFocusModeSource.ShowPrimaryFocusAction => true;
        string IWebDemoFocusModeSource.PrimaryFocusActionText => "Load sample";
        Task IWebDemoFocusModeSource.ExecutePrimaryFocusActionAsync() => RunOpenSampleAsync();
        void IWebDemoFocusModeSource.ExitFocusMode()
        {
            if (!_isFocusMode)
            {
                return;
            }

            _isFocusMode = false;
            UpdateFocusMode();
        }

        event EventHandler<WebDemoFocusModeChangedEventArgs> IWebDemoFocusModeSource.FocusModeChanged
        {
            add => _focusModeChanged += value;
            remove => _focusModeChanged -= value;
        }

        private event EventHandler<WebDemoFocusModeChangedEventArgs> _focusModeChanged;

        public PdfViewerShowcaseView(Window ownerWindow, string theme = "light")
        {
            _ownerWindow = ownerWindow ?? throw new ArgumentNullException(nameof(ownerWindow));
            _theme = NormalizeTheme(theme);

            _loadSampleButton = CreateButton("Load sample");
            _loadSampleButton.Click += async (_, __) => await RunOpenSampleAsync().ConfigureAwait(true);

            _openPdfButton = CreateButton("Open PDF...");
            _openPdfButton.Click += async (_, __) => await RunPickAndOpenAsync().ConfigureAwait(true);

            _expandButton = new Button
            {
                Width = 36,
                Height = 36,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                Content = CreateOverlayIcon(false),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0)
            };
            ToolTipService.SetToolTip(_expandButton, "Expand demo");
            _expandButton.Click += HandleExpandClick;

            _viewer = new PhialePdfViewer(new WinUiWebComponentHostFactory(), new PdfViewerOptions
            {
                InitialTheme = _theme
            })
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Spacing = 8
            };
            buttonRow.Children.Add(_loadSampleButton);
            buttonRow.Children.Add(_openPdfButton);

            _topBar = new Grid
            {
                Margin = new Thickness(18, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };
            _topBar.Children.Add(buttonRow);

            _viewerOverlay = new Grid();
            _viewerOverlay.Children.Add(_viewer);

            _viewerSurface = new Border
            {
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = _viewerOverlay
            };

            _viewerPanel = new Grid
            {
                Width = 504d,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _viewerPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_viewerSurface, 0);
            _viewerPanel.Children.Add(_viewerSurface);
            _viewerPanel.Children.Add(_expandButton);
            Canvas.SetZIndex(_expandButton, 10);

            var root = new Grid();
            root.Children.Add(_viewerPanel);
            root.Children.Add(_topBar);
            Canvas.SetZIndex(_topBar, 20);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            KeyDown += HandleKeyDown;
            SizeChanged += HandleSizeChanged;
            UpdateFocusMode();
        }

        public Task ApplyEnvironmentAsync(string theme)
        {
            _theme = NormalizeTheme(theme);
            if (_disposed || !_opened)
            {
                return Task.CompletedTask;
            }

            return _viewer.SetThemeAsync(_theme);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= HandleLoaded;
            KeyDown -= HandleKeyDown;
            SizeChanged -= HandleSizeChanged;
            _expandButton.Click -= HandleExpandClick;
            _viewer.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if (_opened || _disposed)
            {
                return;
            }

            _opened = true;
            _ = RunOpenAsync();
        }

        private async Task RunOpenAsync()
        {
            await _viewer.InitializeAsync().ConfigureAwait(true);
            await _viewer.SetThemeAsync(_theme).ConfigureAwait(true);
            await RunOpenSampleAsync().ConfigureAwait(true);
        }

        private async Task RunOpenSampleAsync()
        {
            try
            {
                string samplePath = WebAssetLocationResolver.ResolveAssetPath("PdfViewer/Samples/esto-annual-report-2025-preview.pdf");
                await OpenDocumentFitPageAsync(PdfDocumentSource.FromFilePath(samplePath)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ToolTipService.SetToolTip(_viewerSurface, "Failed to open the PDF sample. " + ex.Message);
            }
        }

        private async Task RunPickAndOpenAsync()
        {
            if (_disposed)
            {
                return;
            }

            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".pdf");

            var hwnd = WindowNative.GetWindowHandle(_ownerWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(file.Path))
                {
                    await OpenDocumentFitPageAsync(PdfDocumentSource.FromFilePath(file.Path)).ConfigureAwait(true);
                    return;
                }

                using var randomAccessStream = await file.OpenReadAsync();
                using var stream = randomAccessStream.AsStreamForRead();
                await OpenDocumentFitPageAsync(PdfDocumentSource.FromStream(stream, file.Name, leaveOpen: true)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ToolTipService.SetToolTip(_viewerSurface, "Failed to open the selected PDF. " + ex.Message);
            }
        }

        private async Task OpenDocumentFitPageAsync(PdfDocumentSource source)
        {
            await _viewer.OpenAsync(source).ConfigureAwait(true);
            await _viewer.SetZoomAsync(PdfZoomMode.FitPage).ConfigureAwait(true);
        }

        private void HandleExpandClick(object sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;
            UpdateFocusMode();
        }

        private void HandleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateViewerSurfaceHeight();
        }

        private void HandleKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isFocusMode || e.Key != VirtualKey.Escape)
            {
                return;
            }

            _isFocusMode = false;
            UpdateFocusMode();
            e.Handled = true;
        }

        private void UpdateFocusMode()
        {
            _topBar.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(18, 0, 0, 0);
            _viewerSurface.Margin = new Thickness(0);
            _viewerPanel.Width = _isFocusMode ? double.NaN : 504d;
            _viewerPanel.HorizontalAlignment = _isFocusMode ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
            _expandButton.Content = CreateOverlayIcon(_isFocusMode);
            ToolTipService.SetToolTip(_expandButton, _isFocusMode ? "Exit focus" : "Expand demo");
            UpdateViewerSurfaceHeight();
            _focusModeChanged?.Invoke(this, new WebDemoFocusModeChangedEventArgs(_isFocusMode));
        }

        private void UpdateViewerSurfaceHeight()
        {
            if (_isFocusMode)
            {
                _viewerSurface.Height = double.NaN;
                return;
            }

            var availableHeight = Math.Max(0d, ActualHeight);
            _viewerSurface.Height = Math.Max(360d, availableHeight);
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = 92,
                Content = text
            };
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(theme, "night", StringComparison.OrdinalIgnoreCase)
                ? "dark"
                : "light";
        }

        private static SymbolIcon CreateOverlayIcon(bool isCollapse)
        {
            return new SymbolIcon(isCollapse ? Symbol.BackToWindow : Symbol.FullScreen);
        }
    }
}
