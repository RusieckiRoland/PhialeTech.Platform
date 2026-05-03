using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.PdfViewer.Wpf.Controls;
using PhialeTech.WebHost;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PhialeTech.Components.Wpf
{
    public sealed class PdfViewerShowcaseView : UserControl, IDisposable, IWebDemoFocusModeSource
    {
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

        public PdfViewerShowcaseView(string theme = "light")
        {
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
                ToolTip = "Expand demo",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0)
            };
            _expandButton.Click += HandleExpandClick;

            _viewer = new PhialePdfViewer(new PhialeTech.WebHost.Wpf.WpfWebComponentHostFactory(), new PdfViewerOptions
            {
                InitialTheme = _theme
            })
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var buttonRow = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(18, 0, 0, 0),
                ItemHeight = 36,
                ItemWidth = double.NaN
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

            _viewerOverlay = new Grid
            {
                ClipToBounds = true
            };
            _viewerOverlay.Children.Add(_viewer);

            _viewerSurface = new Border
            {
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                Child = _viewerOverlay
            };

            _viewerPanel = new Grid
            {
                Width = 504d,
                HorizontalAlignment = HorizontalAlignment.Left,
                ClipToBounds = true
            };
            _viewerPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_viewerSurface, 0);
            _viewerPanel.Children.Add(_viewerSurface);
            _viewerPanel.Children.Add(_expandButton);
            Panel.SetZIndex(_expandButton, 10);

            var root = new Grid
            {
                ClipToBounds = true
            };
            root.Children.Add(_viewerPanel);
            root.Children.Add(_topBar);
            Panel.SetZIndex(_topBar, 20);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            PreviewKeyDown += HandlePreviewKeyDown;
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
            PreviewKeyDown -= HandlePreviewKeyDown;
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
                _viewerSurface.ToolTip = "Failed to open the PDF sample. " + ex.Message;
            }
        }

        private async Task RunPickAndOpenAsync()
        {
            if (_disposed)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Select PDF (*.pdf)",
                DefaultExt = ".pdf",
                Filter = "PDF documents (*.pdf)|*.pdf",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            try
            {
                await OpenDocumentFitPageAsync(PdfDocumentSource.FromFilePath(dialog.FileName)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _viewerSurface.ToolTip = "Failed to open the selected PDF. " + ex.Message;
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

        private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isFocusMode || e.Key != Key.Escape)
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
            _expandButton.ToolTip = _isFocusMode ? "Exit focus" : "Expand demo";
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

        private static FrameworkElement CreateOverlayIcon(bool isCollapse)
        {
            var data = isCollapse
                ? "M10 3v7H3 M3 10l7-7 M14 3v7h7 M21 10l-7-7 M10 21v-7H3 M3 14l7 7 M14 21v-7h7 M21 14l-7 7"
                : "M8 3H3v5 M3 3l7 7 M16 3h5v5 M21 3l-7 7 M8 21H3v-5 M3 21l7-7 M16 21h5v-5 M21 21l-7-7";

            var iconPath = new Path
            {
                Data = Geometry.Parse(data),
                StrokeThickness = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            iconPath.SetBinding(Shape.StrokeProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });

            return new Viewbox
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Child = iconPath
            };
        }
    }
}
