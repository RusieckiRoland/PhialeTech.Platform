using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.MonacoEditor.Wpf.Controls
{
    public sealed class PhialeMonacoEditor : UserControl, IMonacoEditor
    {
        private readonly MonacoEditorOptions _options;
        private readonly MonacoEditorWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly MonacoEditorRuntime _runtime;
        private readonly Border _hostBorder;
        private readonly Grid _clipHost;
        private bool _disposed;

        public PhialeMonacoEditor()
            : this(new WpfWebComponentHostFactory(), new MonacoEditorOptions())
        {
        }

        public PhialeMonacoEditor(IWebComponentHostFactory hostFactory, MonacoEditorOptions options = null)
        {
            if (hostFactory == null)
                throw new ArgumentNullException(nameof(hostFactory));

            _options = (options ?? new MonacoEditorOptions()).Clone();
            _workspace = new MonacoEditorWorkspace(_options);
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

            _runtime = new MonacoEditorRuntime(_host, _workspace, _options);

            _clipHost = new Grid
            {
                ClipToBounds = true,
                SnapsToDevicePixels = true,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            };
            _clipHost.Children.Add(hostElement);

            _hostBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                ClipToBounds = true,
                Child = _clipHost
            };

            Content = _hostBorder;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
            ApplyThemeVisuals(_options.InitialTheme);
        }

        public MonacoEditorOptions Options => _options;

        public new bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public string Value => _runtime.Value;

        public new string Language => _runtime.Language;

        public string Theme => _runtime.Theme;

        public event EventHandler<MonacoEditorReadyStateChangedEventArgs> ReadyStateChanged
        {
            add { _runtime.ReadyStateChanged += value; }
            remove { _runtime.ReadyStateChanged -= value; }
        }

        public event EventHandler<MonacoEditorContentChangedEventArgs> ContentChanged
        {
            add { _runtime.ContentChanged += value; }
            remove { _runtime.ContentChanged -= value; }
        }

        public event EventHandler<MonacoEditorErrorEventArgs> ErrorOccurred
        {
            add { _runtime.ErrorOccurred += value; }
            remove { _runtime.ErrorOccurred -= value; }
        }

        public Task InitializeAsync() => _runtime.InitializeAsync();

        public Task SetValueAsync(string value) => _runtime.SetValueAsync(value);

        public Task<string> GetValueAsync() => _runtime.GetValueAsync();

        public Task SetLanguageAsync(string language) => _runtime.SetLanguageAsync(language);

        public async Task SetThemeAsync(string theme)
        {
            ApplyThemeVisuals(theme);
            await _runtime.SetThemeAsync(theme).ConfigureAwait(true);
        }

        public void FocusEditor() => _runtime.FocusEditor();

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Loaded -= HandleLoaded;
            _runtime.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            _ = _runtime.InitializeAsync();
        }

        private void ApplyThemeVisuals(string theme)
        {
            bool useDark = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
            _hostBorder.BorderBrush = new SolidColorBrush(useDark ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225));
            _hostBorder.Background = new SolidColorBrush(useDark ? Color.FromRgb(15, 23, 42) : Color.FromRgb(241, 245, 249));
            _clipHost.Background = new SolidColorBrush(useDark ? Color.FromRgb(15, 23, 42) : Color.FromRgb(241, 245, 249));
        }
    }
}
