using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PhialeTech.ReportDesigner;
using PhialeTech.ReportDesigner.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.WinUI;
using System;
using System.Threading.Tasks;

namespace PhialeTech.ReportDesigner.WinUI.Controls
{
    public sealed class PhialeReportDesigner : UserControl, IReportDesigner
    {
        private enum ShellStatusKind
        {
            Waiting,
            HostInitialized,
            Ready,
            DefinitionChanged,
            PreviewReady,
            DesignMode,
            PreviewMode,
            Error
        }

        private readonly ReportDesignerOptions _options;
        private readonly ReportDesignerWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly ReportDesignerRuntime _runtime;
        private readonly Button _designButton;
        private readonly Button _previewButton;
        private readonly Button _refreshButton;
        private readonly Button _printButton;
        private readonly Button _focusButton;
        private readonly TextBlock _statusText;
        private readonly Border _hostBorder;
        private ReportDesignerMode _mode;
        private string _locale;
        private string _theme;
        private ShellStatusKind _statusKind = ShellStatusKind.Waiting;
        private int _lastDefinitionBlockCount;
        private int _lastPreviewPageCount;
        private bool _lastPreviewUsedSampleData;
        private string _lastErrorMessage = string.Empty;
        private bool _disposed;

        public PhialeReportDesigner()
            : this(new WinUiWebComponentHostFactory(), new ReportDesignerOptions())
        {
        }

        public PhialeReportDesigner(IWebComponentHostFactory hostFactory, ReportDesignerOptions? options = null)
        {
            if (hostFactory is null)
                throw new ArgumentNullException(nameof(hostFactory));

            _options = (options ?? new ReportDesignerOptions()).Clone();
            _locale = ReportDesignerShellTextCatalog.NormalizeLocale(_options.InitialLocale);
            _theme = string.Equals(_options.InitialTheme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
            _workspace = new ReportDesignerWorkspace(_options);
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

            _runtime = new ReportDesignerRuntime(_host, _workspace, _options);
            _runtime.ReadyStateChanged += HandleReadyStateChanged;
            _runtime.DefinitionChanged += HandleDefinitionChanged;
            _runtime.PreviewReady += HandlePreviewReady;
            _runtime.ModeChanged += HandleModeChanged;
            _runtime.ErrorOccurred += HandleErrorOccurred;

            _designButton = CreateButton();
            _designButton.Click += async (_, __) => await RunUiActionAsync(() => SetModeAsync(ReportDesignerMode.Design), "Error.SwitchDesign");

            _previewButton = CreateButton();
            _previewButton.Click += async (_, __) => await RunUiActionAsync(() => SetModeAsync(ReportDesignerMode.Preview), "Error.SwitchPreview");

            _refreshButton = CreateButton();
            _refreshButton.Click += async (_, __) => await RunUiActionAsync(RefreshPreviewAsync, "Error.RefreshPreview");

            _printButton = CreateButton();
            _printButton.Click += async (_, __) => await RunUiActionAsync(PrintAsync, "Error.Print");

            _focusButton = CreateButton();
            _focusButton.Click += (_, __) => FocusDesigner();

            _statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            };

            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            toolbarPanel.Children.Add(_designButton);
            toolbarPanel.Children.Add(_previewButton);
            toolbarPanel.Children.Add(_refreshButton);
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

            _hostBorder = new Border
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
            Grid.SetRow(_hostBorder, 2);
            root.Children.Add(toolbarScroller);
            root.Children.Add(_statusText);
            root.Children.Add(_hostBorder);

            Content = root;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            ApplyLocalizedTexts();
            ApplyThemeVisuals();
            RefreshStatusText();
            UpdateUiState();
        }

        public ReportDesignerOptions Options => _options;

        public bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public ReportDesignerMode Mode => _mode;

        public string Locale => _locale;

        public string Theme => _theme;

        public event EventHandler<ReportDesignerReadyStateChangedEventArgs> ReadyStateChanged
        {
            add => _runtime.ReadyStateChanged += value;
            remove => _runtime.ReadyStateChanged -= value;
        }

        public event EventHandler<ReportDefinitionChangedEventArgs> DefinitionChanged
        {
            add => _runtime.DefinitionChanged += value;
            remove => _runtime.DefinitionChanged -= value;
        }

        public event EventHandler<ReportPreviewReadyEventArgs> PreviewReady
        {
            add => _runtime.PreviewReady += value;
            remove => _runtime.PreviewReady -= value;
        }

        public event EventHandler<ReportDesignerModeChangedEventArgs> ModeChanged
        {
            add => _runtime.ModeChanged += value;
            remove => _runtime.ModeChanged -= value;
        }

        public event EventHandler<ReportDesignerErrorEventArgs> ErrorOccurred
        {
            add => _runtime.ErrorOccurred += value;
            remove => _runtime.ErrorOccurred -= value;
        }

        public Task InitializeAsync() => _runtime.InitializeAsync();

        public Task SetModeAsync(ReportDesignerMode mode) => _runtime.SetModeAsync(mode);

        public async Task SetLocaleAsync(string locale)
        {
            _locale = ReportDesignerShellTextCatalog.NormalizeLocale(locale);
            ApplyLocalizedTexts();
            RefreshStatusText();
            await _runtime.SetLocaleAsync(_locale).ConfigureAwait(true);
        }

        public async Task SetThemeAsync(string theme)
        {
            _theme = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
            ApplyThemeVisuals();
            UpdateUiState();
            await _runtime.SetThemeAsync(_theme).ConfigureAwait(true);
        }

        public Task LoadDefinitionAsync(ReportDefinition definition) => _runtime.LoadDefinitionAsync(definition);

        public Task<ReportDefinition> GetDefinitionAsync() => _runtime.GetDefinitionAsync();

        public Task SetDataSchemaAsync(ReportDataSchema schema) => _runtime.SetDataSchemaAsync(schema);

        public Task SetSampleDataAsync(string json) => _runtime.SetSampleDataAsync(json);

        public Task SetReportDataAsync(string json) => _runtime.SetReportDataAsync(json);

        public Task RefreshPreviewAsync() => _runtime.RefreshPreviewAsync();

        public Task PrintAsync() => _runtime.PrintAsync();

        public void FocusDesigner()
        {
            if (_disposed)
                return;

            _runtime.FocusDesigner();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Loaded -= HandleLoaded;
            _runtime.ReadyStateChanged -= HandleReadyStateChanged;
            _runtime.DefinitionChanged -= HandleDefinitionChanged;
            _runtime.PreviewReady -= HandlePreviewReady;
            _runtime.ModeChanged -= HandleModeChanged;
            _runtime.ErrorOccurred -= HandleErrorOccurred;
            _runtime.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            _ = RunUiActionAsync(InitializeAsync, "Error.Initialize");
        }

        private void HandleReadyStateChanged(object? sender, ReportDesignerReadyStateChangedEventArgs e)
        {
            _statusKind = e.IsReady
                ? ShellStatusKind.Ready
                : e.IsInitialized
                    ? ShellStatusKind.HostInitialized
                    : ShellStatusKind.Waiting;
            RefreshStatusText();
            UpdateUiState();
        }

        private void HandleDefinitionChanged(object? sender, ReportDefinitionChangedEventArgs e)
        {
            _lastDefinitionBlockCount = e.Definition?.Blocks?.Count ?? 0;
            _statusKind = ShellStatusKind.DefinitionChanged;
            RefreshStatusText();
        }

        private void HandlePreviewReady(object? sender, ReportPreviewReadyEventArgs e)
        {
            _lastPreviewPageCount = e.PageCount;
            _lastPreviewUsedSampleData = e.UsedSampleData;
            _statusKind = ShellStatusKind.PreviewReady;
            RefreshStatusText();
        }

        private void HandleModeChanged(object? sender, ReportDesignerModeChangedEventArgs e)
        {
            _mode = e.Mode;
            _statusKind = e.Mode == ReportDesignerMode.Preview
                ? ShellStatusKind.PreviewMode
                : ShellStatusKind.DesignMode;
            RefreshStatusText();
            UpdateUiState();
        }

        private void HandleErrorOccurred(object? sender, ReportDesignerErrorEventArgs e)
        {
            string message = string.IsNullOrWhiteSpace(e.Message) ? "ReportDesigner error" : e.Message;
            if (!string.IsNullOrWhiteSpace(e.Detail))
                message += " " + e.Detail;

            _lastErrorMessage = message;
            _statusKind = ShellStatusKind.Error;
            RefreshStatusText();
        }

        private async Task RunUiActionAsync(Func<Task> action, string errorKey)
        {
            if (_disposed)
                return;

            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ReportDesignerShellTextCatalog.GetText(_locale, errorKey) + " " + ex.Message;
                _statusKind = ShellStatusKind.Error;
                RefreshStatusText();
            }
        }

        private void UpdateUiState()
        {
            bool canInteract = IsInitialized;
            _designButton.IsEnabled = canInteract;
            _previewButton.IsEnabled = canInteract;
            _refreshButton.IsEnabled = IsReady;
            _printButton.IsEnabled = IsReady;
            _focusButton.IsEnabled = canInteract;

            ApplyModeButtonVisual(_designButton, _mode == ReportDesignerMode.Design, _theme);
            ApplyModeButtonVisual(_previewButton, _mode == ReportDesignerMode.Preview, _theme);
        }

        private void RefreshStatusText()
        {
            switch (_statusKind)
            {
                case ShellStatusKind.HostInitialized:
                    _statusText.Text = ReportDesignerShellTextCatalog.GetText(_locale, "Status.HostInitialized");
                    break;

                case ShellStatusKind.Ready:
                    _statusText.Text = ReportDesignerShellTextCatalog.GetText(_locale, "Status.Ready");
                    break;

                case ShellStatusKind.DefinitionChanged:
                    _statusText.Text = ReportDesignerShellTextCatalog.Format(_locale, "Status.DefinitionChanged", _lastDefinitionBlockCount);
                    break;

                case ShellStatusKind.PreviewReady:
                    string sourceLabel = ReportDesignerShellTextCatalog.GetText(
                        _locale,
                        _lastPreviewUsedSampleData ? "Status.PreviewSource.Sample" : "Status.PreviewSource.Report");
                    _statusText.Text = ReportDesignerShellTextCatalog.Format(_locale, "Status.PreviewReady", _lastPreviewPageCount, sourceLabel);
                    break;

                case ShellStatusKind.DesignMode:
                    _statusText.Text = ReportDesignerShellTextCatalog.GetText(_locale, "Status.Mode.Design");
                    break;

                case ShellStatusKind.PreviewMode:
                    _statusText.Text = ReportDesignerShellTextCatalog.GetText(_locale, "Status.Mode.Preview");
                    break;

                case ShellStatusKind.Error:
                    _statusText.Text = ReportDesignerShellTextCatalog.GetText(_locale, "Status.ErrorPrefix") + (_lastErrorMessage ?? string.Empty);
                    break;

                default:
                    _statusText.Text = ReportDesignerShellTextCatalog.GetText(_locale, "Status.Waiting");
                    break;
            }
        }

        private void ApplyLocalizedTexts()
        {
            _designButton.Content = ReportDesignerShellTextCatalog.GetText(_locale, "Button.Design");
            _previewButton.Content = ReportDesignerShellTextCatalog.GetText(_locale, "Button.Preview");
            _refreshButton.Content = ReportDesignerShellTextCatalog.GetText(_locale, "Button.RefreshPreview");
            _printButton.Content = ReportDesignerShellTextCatalog.GetText(_locale, "Button.Print");
            _focusButton.Content = ReportDesignerShellTextCatalog.GetText(_locale, "Button.Focus");
        }

        private void ApplyThemeVisuals()
        {
            bool useDark = string.Equals(_theme, "dark", StringComparison.OrdinalIgnoreCase);
            _statusText.Foreground = new SolidColorBrush(useDark ? Colors.LightGray : Colors.SlateGray);
            _hostBorder.BorderBrush = new SolidColorBrush(useDark ? ColorHelper.FromArgb(255, 51, 65, 85) : Colors.LightGray);
            _hostBorder.Background = new SolidColorBrush(useDark ? ColorHelper.FromArgb(255, 15, 23, 42) : Colors.WhiteSmoke);
        }

        private static Button CreateButton()
        {
            return new Button
            {
                Margin = new Thickness(0, 0, 6, 6),
                Padding = new Thickness(10, 6, 10, 6),
                MinWidth = 52,
            };
        }

        private static void ApplyModeButtonVisual(Button button, bool isActive, string theme)
        {
            if (button == null)
                return;

            button.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
            button.Background = isActive
                ? new SolidColorBrush(string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
                    ? ColorHelper.FromArgb(255, 22, 78, 99)
                    : Colors.LightBlue)
                : null;
            button.Foreground = new SolidColorBrush(string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
                ? Colors.White
                : ColorHelper.FromArgb(255, 17, 24, 39));
        }
    }
}
