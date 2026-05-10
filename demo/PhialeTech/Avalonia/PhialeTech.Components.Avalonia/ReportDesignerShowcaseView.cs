using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using PhialeTech.Components.Shared.Services;
using PhialeTech.ReportDesigner.Abstractions;
using PhialeTech.ReportDesigner.Avalonia.Controls;
using PhialeTech.WebHost.Avalonia;
using System;
using System.Threading.Tasks;

namespace PhialeTech.Components.Avalonia
{
    public sealed class ReportDesignerShowcaseView : UserControl, IDisposable
    {
        private readonly PhialeReportDesigner _designer;
        private readonly TextBlock _headlineText;
        private readonly TextBlock _descriptionText;
        private readonly Button _loadSampleButton;
        private readonly Button _expandButton;
        private readonly StackPanel _titlePanel;
        private readonly Grid _topBar;
        private readonly StackPanel _detailsPanel;
        private readonly Border _designerSurface;
        private string _languageCode;
        private string _theme;
        private bool _opened;
        private bool _disposed;
        private bool _isFocusMode;

        public ReportDesignerShowcaseView(string languageCode = "en", string theme = "light")
        {
            DemoCefRuntime.EnsureInitialized();

            _languageCode = ReportDesignerShowcaseTextCatalog.NormalizeLanguage(languageCode);
            _theme = NormalizeTheme(theme);

            _headlineText = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeight.SemiBold,
            };

            _descriptionText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            };

            _loadSampleButton = CreateButton(string.Empty);
            _loadSampleButton.Click += async (_, __) => await RunLoadSampleAsync().ConfigureAwait(true);

            _expandButton = CreateButton(string.Empty);
            _expandButton.Click += HandleExpandClick;

            _designer = new PhialeReportDesigner(
                new AvaloniaWebComponentHostFactory(),
                new ReportDesignerOptions
                {
                    InitialLocale = _languageCode,
                    InitialTheme = _theme,
                })
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

            _designerSurface = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true,
                Child = _designer
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
            Grid.SetRow(_designerSurface, 2);
            root.Children.Add(_topBar);
            root.Children.Add(_detailsPanel);
            root.Children.Add(_designerSurface);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            AttachedToVisualTree += HandleAttachedToVisualTree;
            KeyDown += HandleKeyDown;
            UpdateLocalizedTexts();
            UpdateFocusMode();
        }

        public async Task ApplyEnvironmentAsync(string languageCode, string theme)
        {
            _languageCode = ReportDesignerShowcaseTextCatalog.NormalizeLanguage(languageCode);
            _theme = NormalizeTheme(theme);
            UpdateLocalizedTexts();

            if (_disposed || !_opened)
            {
                return;
            }

            await _designer.SetLocaleAsync(_languageCode).ConfigureAwait(true);
            await _designer.SetThemeAsync(_theme).ConfigureAwait(true);
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
            _designer.Dispose();
        }

        private void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_opened || _disposed)
            {
                return;
            }

            _opened = true;
            _ = RunLoadSampleAsync();
        }

        private async Task RunLoadSampleAsync()
        {
            try
            {
                await _designer.InitializeAsync().ConfigureAwait(true);
                await _designer.SetLocaleAsync(_languageCode).ConfigureAwait(true);
                await _designer.SetThemeAsync(_theme).ConfigureAwait(true);
                await _designer.SetDataSchemaAsync(DemoReportDesignerSampleBuilder.CreateSchema(_languageCode)).ConfigureAwait(true);
                await _designer.SetSampleDataAsync(DemoReportDesignerSampleBuilder.CreateSampleDataJson(_languageCode)).ConfigureAwait(true);
                await _designer.SetReportDataAsync(DemoReportDesignerSampleBuilder.CreateReportDataJson(_languageCode)).ConfigureAwait(true);
                await _designer.LoadDefinitionAsync(DemoReportDesignerSampleBuilder.CreateDefinition(_languageCode)).ConfigureAwait(true);
                await _designer.SetModeAsync(ReportDesignerMode.Design).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _descriptionText.Text = ReportDesignerShowcaseTextCatalog.GetText(_languageCode, "LoadError") + " " + ex.Message;
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

        private void UpdateLocalizedTexts()
        {
            _headlineText.Text = ReportDesignerShowcaseTextCatalog.GetText(_languageCode, "Headline");
            _descriptionText.Text = ReportDesignerShowcaseTextCatalog.GetText(_languageCode, "Description");
            _loadSampleButton.Content = ReportDesignerShowcaseTextCatalog.GetText(_languageCode, "LoadSample");
            _expandButton.Content = _isFocusMode
                ? ReportDesignerShowcaseTextCatalog.GetText(_languageCode, "ExitFocus")
                : ReportDesignerShowcaseTextCatalog.GetText(_languageCode, "Expand");
        }

        private void UpdateFocusMode()
        {
            _titlePanel.IsVisible = !_isFocusMode;
            _detailsPanel.IsVisible = !_isFocusMode;
            _topBar.Margin = _isFocusMode ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 12);
            _designerSurface.Margin = _isFocusMode ? new Thickness(0) : new Thickness(0, 12, 0, 0);
            UpdateLocalizedTexts();
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
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

