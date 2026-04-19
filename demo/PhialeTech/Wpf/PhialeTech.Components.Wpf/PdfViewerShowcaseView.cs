using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.PdfViewer.Wpf.Controls;
using PhialeTech.WebHost;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhialeTech.Components.Wpf
{
    public sealed class PdfViewerShowcaseView : UserControl, IDisposable, IWebDemoFocusModeSource
    {
        private readonly PhialePdfViewer _viewer;
        private readonly TextBlock _headlineText;
        private readonly TextBlock _descriptionText;
        private readonly Button _loadSampleButton;
        private readonly Button _openPdfButton;
        private readonly Button _expandButton;
        private readonly StackPanel _titlePanel;
        private readonly Grid _topBar;
        private readonly StackPanel _detailsPanel;
        private readonly Border _viewerSurface;
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

        public PdfViewerShowcaseView()
        {
            _headlineText = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Text = "PdfViewer demo with native toolbar"
            };

            _descriptionText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Text = "This showcase keeps the toolbar native to WPF and uses the neutral PhialeTech WebHost only as the browser surface for PDF.js."
            };

            _loadSampleButton = CreateButton("Load sample");
            _loadSampleButton.Click += async (_, __) => await RunOpenSampleAsync().ConfigureAwait(true);

            _openPdfButton = CreateButton("Open PDF...");
            _openPdfButton.Click += async (_, __) => await RunPickAndOpenAsync().ConfigureAwait(true);

            _expandButton = new Button
            {
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = 108,
                Content = "Expand demo",
                Background = new SolidColorBrush(Color.FromArgb(188, 15, 23, 42)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(96, 148, 163, 184)),
                BorderThickness = new Thickness(1)
            };
            _expandButton.Click += HandleExpandClick;

            _viewer = new PhialePdfViewer
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
            buttonRow.Children.Add(_expandButton);

            _titlePanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    _headlineText
                }
            };

            _topBar = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            _topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_titlePanel, 0);
            Grid.SetColumn(buttonRow, 1);
            _topBar.Children.Add(_titlePanel);
            _topBar.Children.Add(buttonRow);

            _detailsPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12),
                Children =
                {
                    _descriptionText
                }
            };

            _viewerSurface = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                Child = _viewer
            };

            var root = new Grid
            {
                ClipToBounds = true
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_topBar, 0);
            Grid.SetRow(_detailsPanel, 1);
            Grid.SetRow(_viewerSurface, 2);
            root.Children.Add(_topBar);
            root.Children.Add(_detailsPanel);
            root.Children.Add(_viewerSurface);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            PreviewKeyDown += HandlePreviewKeyDown;
            UpdateFocusMode();
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
            await RunOpenSampleAsync().ConfigureAwait(true);
        }

        private async Task RunOpenSampleAsync()
        {
            try
            {
                string samplePath = WebAssetLocationResolver.ResolveAssetPath("PdfViewer/Samples/phialetech-sample.pdf");
                await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(samplePath)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _descriptionText.Text = "Failed to open the PDF sample. " + ex.Message;
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
                await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(dialog.FileName)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _descriptionText.Text = "Failed to open the selected PDF. " + ex.Message;
            }
        }

        private void HandleExpandClick(object sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;
            UpdateFocusMode();
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
            _titlePanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _detailsPanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
            _viewerSurface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
            _expandButton.Content = _isFocusMode ? "Exit focus" : "Expand demo";
            _focusModeChanged?.Invoke(this, new WebDemoFocusModeChangedEventArgs(_isFocusMode));
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
    }
}
