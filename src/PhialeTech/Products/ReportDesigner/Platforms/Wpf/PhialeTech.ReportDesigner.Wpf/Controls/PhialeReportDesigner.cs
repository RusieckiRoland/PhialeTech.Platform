using PhialeTech.ReportDesigner.Abstractions;
using PhialeTech.ReportDesigner;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.ReportDesigner.Wpf.Controls
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
            : this(new WpfWebComponentHostFactory(), new ReportDesignerOptions())
        {
        }

        public PhialeReportDesigner(IWebComponentHostFactory hostFactory, ReportDesignerOptions options = null)
        {
            if (hostFactory == null)
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
            if (hostElement == null)
                throw new InvalidOperationException("The supplied WPF web host factory did not return a WPF UI element.");

            _runtime = new ReportDesignerRuntime(_host, _workspace, _options);
            _runtime.ReadyStateChanged += HandleReadyStateChanged;
            _runtime.DefinitionChanged += HandleDefinitionChanged;
            _runtime.PreviewReady += HandlePreviewReady;
            _runtime.ModeChanged += HandleModeChanged;
            _runtime.ErrorOccurred += HandleErrorOccurred;

            _designButton = CreateButton();
            WireToolbarAction(_designButton, () => SetModeAsync(ReportDesignerMode.Design), "Error.SwitchDesign");

            _previewButton = CreateButton();
            WireToolbarAction(_previewButton, () => SetModeAsync(ReportDesignerMode.Preview), "Error.SwitchPreview");

            _refreshButton = CreateButton();
            WireToolbarAction(_refreshButton, RefreshPreviewAsync, "Error.RefreshPreview");

            _printButton = CreateButton();
            WireToolbarAction(_printButton, PrintAsync, "Error.Print");

            _focusButton = CreateButton();
            _focusButton.Click += (_, __) => FocusDesigner();

            _statusText = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            };

            var toolbarPanel = new WrapPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            toolbarPanel.Children.Add(_designButton);
            toolbarPanel.Children.Add(_previewButton);
            toolbarPanel.Children.Add(_refreshButton);
            toolbarPanel.Children.Add(_printButton);
            toolbarPanel.Children.Add(_focusButton);

            _hostBorder = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Child = hostElement,
                ClipToBounds = true
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(toolbarPanel, 0);
            Grid.SetRow(_statusText, 1);
            Grid.SetRow(_hostBorder, 2);
            root.Children.Add(toolbarPanel);
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

        public new bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public ReportDesignerMode Mode => _mode;

        public string Locale => _locale;

        public string Theme => _theme;

        public event EventHandler<ReportDesignerReadyStateChangedEventArgs> ReadyStateChanged
        {
            add { _runtime.ReadyStateChanged += value; }
            remove { _runtime.ReadyStateChanged -= value; }
        }

        public event EventHandler<ReportDefinitionChangedEventArgs> DefinitionChanged
        {
            add { _runtime.DefinitionChanged += value; }
            remove { _runtime.DefinitionChanged -= value; }
        }

        public event EventHandler<ReportPreviewReadyEventArgs> PreviewReady
        {
            add { _runtime.PreviewReady += value; }
            remove { _runtime.PreviewReady -= value; }
        }

        public event EventHandler<ReportDesignerModeChangedEventArgs> ModeChanged
        {
            add { _runtime.ModeChanged += value; }
            remove { _runtime.ModeChanged -= value; }
        }

        public event EventHandler<ReportDesignerErrorEventArgs> ErrorOccurred
        {
            add { _runtime.ErrorOccurred += value; }
            remove { _runtime.ErrorOccurred -= value; }
        }

        public System.Threading.Tasks.Task InitializeAsync() => _runtime.InitializeAsync();

        public System.Threading.Tasks.Task SetModeAsync(ReportDesignerMode mode) => _runtime.SetModeAsync(mode);

        public async System.Threading.Tasks.Task SetLocaleAsync(string locale)
        {
            _locale = ReportDesignerShellTextCatalog.NormalizeLocale(locale);
            ApplyLocalizedTexts();
            RefreshStatusText();
            await _runtime.SetLocaleAsync(_locale).ConfigureAwait(true);
        }

        public async System.Threading.Tasks.Task SetThemeAsync(string theme)
        {
            _theme = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
            ApplyThemeVisuals();
            UpdateUiState();
            await _runtime.SetThemeAsync(_theme).ConfigureAwait(true);
        }

        public System.Threading.Tasks.Task LoadDefinitionAsync(ReportDefinition definition) => _runtime.LoadDefinitionAsync(definition);

        public System.Threading.Tasks.Task<ReportDefinition> GetDefinitionAsync() => _runtime.GetDefinitionAsync();

        public System.Threading.Tasks.Task SetDataSchemaAsync(ReportDataSchema schema) => _runtime.SetDataSchemaAsync(schema);

        public System.Threading.Tasks.Task SetSampleDataAsync(string json) => _runtime.SetSampleDataAsync(json);

        public System.Threading.Tasks.Task SetReportDataAsync(string json) => _runtime.SetReportDataAsync(json);

        public System.Threading.Tasks.Task RefreshPreviewAsync() => _runtime.RefreshPreviewAsync();

        public System.Threading.Tasks.Task PrintAsync() => _runtime.PrintAsync();

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

        private void HandleReadyStateChanged(object sender, ReportDesignerReadyStateChangedEventArgs e)
        {
            _statusKind = e.IsReady
                ? ShellStatusKind.Ready
                : e.IsInitialized
                    ? ShellStatusKind.HostInitialized
                    : ShellStatusKind.Waiting;
            RefreshStatusText();
            UpdateUiState();
        }

        private void HandleDefinitionChanged(object sender, ReportDefinitionChangedEventArgs e)
        {
            _lastDefinitionBlockCount = e.Definition?.Blocks?.Count ?? 0;
            _statusKind = ShellStatusKind.DefinitionChanged;
            RefreshStatusText();
        }

        private void HandlePreviewReady(object sender, ReportPreviewReadyEventArgs e)
        {
            _lastPreviewPageCount = e.PageCount;
            _lastPreviewUsedSampleData = e.UsedSampleData;
            _statusKind = ShellStatusKind.PreviewReady;
            RefreshStatusText();
        }

        private void HandleModeChanged(object sender, ReportDesignerModeChangedEventArgs e)
        {
            _mode = e.Mode;
            _statusKind = e.Mode == ReportDesignerMode.Preview
                ? ShellStatusKind.PreviewMode
                : ShellStatusKind.DesignMode;
            RefreshStatusText();
            UpdateUiState();
        }

        private void HandleErrorOccurred(object sender, ReportDesignerErrorEventArgs e)
        {
            string message = string.IsNullOrWhiteSpace(e.Message) ? "ReportDesigner error" : e.Message;
            if (!string.IsNullOrWhiteSpace(e.Detail))
                message += " " + e.Detail;

            _lastErrorMessage = message;
            _statusKind = ShellStatusKind.Error;
            RefreshStatusText();
        }

        private async System.Threading.Tasks.Task RunUiActionAsync(Func<System.Threading.Tasks.Task> action, string errorKey)
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

        private void WireToolbarAction(Button button, Func<System.Threading.Tasks.Task> action, string errorKey)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            bool suppressNextClick = false;

            button.PreviewMouseLeftButtonDown += async (_, e) =>
            {
                if (!button.IsEnabled)
                    return;

                suppressNextClick = true;
                e.Handled = true;
                await RunUiActionAsync(action, errorKey).ConfigureAwait(true);
            };

            button.Click += async (_, __) =>
            {
                if (suppressNextClick)
                {
                    suppressNextClick = false;
                    return;
                }

                await RunUiActionAsync(action, errorKey).ConfigureAwait(true);
            };
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
            _statusText.Foreground = new SolidColorBrush(useDark ? Color.FromRgb(203, 213, 225) : Color.FromRgb(71, 85, 105));
            _hostBorder.BorderBrush = new SolidColorBrush(useDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225));
            _hostBorder.Background = new SolidColorBrush(useDark ? Color.FromRgb(15, 23, 42) : Color.FromRgb(241, 245, 249));
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
                    ? Color.FromRgb(22, 78, 99)
                    : Color.FromRgb(224, 242, 254))
                : null;
            button.Foreground = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
                ? Brushes.White
                : Brushes.Black;
        }
    }
}
