using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.MonacoEditor.Wpf.Controls
{
    public sealed class PhialeMonacoEditor : UserControl, IMonacoEditor
    {
        private readonly MonacoEditorOptions _options;
        private readonly MonacoEditorWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly MonacoEditorRuntime _runtime;
        private readonly Grid _hostRoot;
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

            _hostRoot = new Grid
            {
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _hostRoot.Children.Add(hostElement);

            Content = _hostRoot;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Loaded += HandleLoaded;
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
    }
}
