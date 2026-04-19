using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.PdfViewer.Avalonia.Controls;
using PhialeTech.WebHost;
using System;
using System.Threading.Tasks;

namespace PhialeTech.Components.Avalonia
{
    public sealed class PdfViewerShowcaseView : UserControl, IDisposable
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

        public PdfViewerShowcaseView()
        {
            DemoCefRuntime.EnsureInitialized();

            _headlineText = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeight.SemiBold,
                Text = "PdfViewer demo with native toolbar"
            };

            _descriptionText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#52606D")),
                Text = "This showcase keeps the toolbar native to Avalonia and uses the neutral PhialeTech WebHost only as the browser surface for PDF.js."
            };

            _loadSampleButton = CreateButton("Load sample");
            _loadSampleButton.Click += async (_, __) => await RunOpenSampleAsync().ConfigureAwait(true);

            _openPdfButton = CreateButton("Open PDF...");
            _openPdfButton.Click += async (_, __) => await RunPickAndOpenAsync().ConfigureAwait(true);

            _expandButton = new Button
            {
                Content = "Expand demo",
                Padding = new Thickness(12, 6),
                MinWidth = 108,
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

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(18, 0, 0, 0)
            };
            buttonRow.Children.Add(_loadSampleButton);
            buttonRow.Children.Add(_openPdfButton);
            buttonRow.Children.Add(_expandButton);

            _titlePanel = new StackPanel
            {
                Spacing = 6
            };
            _titlePanel.Children.Add(_headlineText);

            _topBar = new Grid
            {
                Margin = new Thickness(0, 0, 0, 12),
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };
            Grid.SetColumn(_titlePanel, 0);
            Grid.SetColumn(buttonRow, 1);
            _topBar.Children.Add(_titlePanel);
            _topBar.Children.Add(buttonRow);

            _detailsPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12),
                Spacing = 10
            };
            _detailsPanel.Children.Add(_descriptionText);

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
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            root.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            Grid.SetRow(_topBar, 0);
            Grid.SetRow(_detailsPanel, 1);
            Grid.SetRow(_viewerSurface, 2);
            root.Children.Add(_topBar);
            root.Children.Add(_detailsPanel);
            root.Children.Add(_viewerSurface);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            AttachedToVisualTree += HandleAttachedToVisualTree;
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
            AttachedToVisualTree -= HandleAttachedToVisualTree;
            KeyDown -= HandleKeyDown;
            _expandButton.Click -= HandleExpandClick;
            _viewer.Dispose();
        }

        private void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
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

            if (TopLevel.GetTopLevel(this) is not TopLevel topLevel || topLevel.StorageProvider is null)
            {
                _descriptionText.Text = "Failed to open the file picker. Storage provider is unavailable.";
                return;
            }

            var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select PDF (*.pdf)",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PDF documents (*.pdf)") { Patterns = new[] { "*.pdf" } },
                    FilePickerFileTypes.All
                }
            }).ConfigureAwait(true);

            if (result == null || result.Count == 0)
            {
                return;
            }

            var picked = result[0];

            try
            {
                string? localPath = picked.TryGetLocalPath();
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(localPath)).ConfigureAwait(true);
                    return;
                }

                await using var stream = await picked.OpenReadAsync().ConfigureAwait(true);
                await _viewer.OpenAsync(PdfDocumentSource.FromStream(stream, picked.Name, leaveOpen: true)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _descriptionText.Text = "Failed to open the selected PDF. " + ex.Message;
            }
        }

        private void HandleExpandClick(object? sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;
            UpdateFocusMode();
        }

        private void HandleKeyDown(object? sender, KeyEventArgs e)
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
            _titlePanel.IsVisible = !_isFocusMode;
            _detailsPanel.IsVisible = !_isFocusMode;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
            _viewerSurface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
            _expandButton.Content = _isFocusMode ? "Exit focus" : "Expand demo";
        }

        private static Button CreateButton(string text)
        {
            return new Button
            {
                Padding = new Thickness(12, 6),
                MinWidth = 92,
                Content = text
            };
        }
    }
}
