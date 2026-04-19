using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.PdfViewer.WinUI.Controls;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PhialeTech.Components.WinUI
{
    public sealed class PdfViewerShowcaseView : UserControl, IDisposable
    {
        private readonly Window _ownerWindow;
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

        public PdfViewerShowcaseView(Window ownerWindow)
        {
            _ownerWindow = ownerWindow ?? throw new ArgumentNullException(nameof(ownerWindow));

            _headlineText = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Text = "PdfViewer demo with native toolbar"
            };

            _descriptionText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = "This showcase keeps the toolbar native to WinUI and uses the neutral PhialeTech WebHost only as the browser surface for PDF.js."
            };

            _loadSampleButton = CreateButton("Load sample");
            _loadSampleButton.Click += async (_, __) => await RunOpenSampleAsync().ConfigureAwait(true);

            _openPdfButton = CreateButton("Open PDF...");
            _openPdfButton.Click += async (_, __) => await RunPickAndOpenAsync().ConfigureAwait(true);

            _expandButton = new Button
            {
                Content = "Expand demo",
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = 108,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(188, 15, 23, 42)),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(96, 148, 163, 184)),
                BorderThickness = new Thickness(1)
            };
            _expandButton.Click += HandleExpandClick;

            _viewer = new PhialePdfViewer
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
            };
            buttonRow.Children.Add(_loadSampleButton);
            buttonRow.Children.Add(_openPdfButton);
            buttonRow.Children.Add(_expandButton);

            _titlePanel = new StackPanel
            {
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
                Child = _viewer
            };

            var root = new Grid();
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
            KeyDown += HandleKeyDown;
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
            KeyDown -= HandleKeyDown;
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
                    await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(file.Path)).ConfigureAwait(true);
                    return;
                }

                using var randomAccessStream = await file.OpenReadAsync();
                using var stream = randomAccessStream.AsStreamForRead();
                await _viewer.OpenAsync(PdfDocumentSource.FromStream(stream, file.Name, leaveOpen: true)).ConfigureAwait(true);
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

        private void HandleKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isFocusMode || e.Key != Windows.System.VirtualKey.Escape)
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
            _detailsPanel.Visibility = _isFocusMode ? Visibility.Collapsed : Visibility.Visible;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
            _viewerSurface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
            _expandButton.Content = _isFocusMode ? "Exit focus" : "Expand demo";
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = 92,
                Content = text
            };
        }
    }
}
