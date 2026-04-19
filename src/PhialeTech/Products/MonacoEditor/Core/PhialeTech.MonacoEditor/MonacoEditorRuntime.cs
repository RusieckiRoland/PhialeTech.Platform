using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.MonacoEditor
{
    public sealed class MonacoEditorRuntime : IDisposable
    {
        private readonly IWebComponentHost _host;
        private readonly MonacoEditorWorkspace _workspace;
        private readonly MonacoEditorOptions _options;
        private Task _initializeTask;
        private bool _disposed;
        private string _value;
        private string _language;
        private string _theme;

        public MonacoEditorRuntime(
            IWebComponentHost host,
            MonacoEditorWorkspace workspace,
            MonacoEditorOptions options)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _options = (options ?? new MonacoEditorOptions()).Clone();
            _value = _options.InitialValue ?? string.Empty;
            _language = NormalizeLanguage(_options.InitialLanguage);
            _theme = NormalizeTheme(_options.InitialTheme);

            _host.ReadyStateChanged += HandleHostReadyStateChanged;
            _host.MessageReceived += HandleHostMessageReceived;
        }

        public bool IsInitialized => _host.IsInitialized;

        public bool IsReady => _host.IsReady;

        public string Value => _value;

        public string Language => _language;

        public string Theme => _theme;

        public event EventHandler<MonacoEditorReadyStateChangedEventArgs> ReadyStateChanged;

        public event EventHandler<MonacoEditorContentChangedEventArgs> ContentChanged;

        public event EventHandler<MonacoEditorErrorEventArgs> ErrorOccurred;

        public Task InitializeAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MonacoEditorRuntime));

            if (_initializeTask == null)
                _initializeTask = InitializeCoreAsync();

            return _initializeTask;
        }

        public async Task SetValueAsync(string value)
        {
            _value = value ?? string.Empty;
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new
            {
                type = "monaco.setValue",
                value = _value
            }).ConfigureAwait(false);
        }

        public async Task<string> GetValueAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            return _value;
        }

        public async Task SetLanguageAsync(string language)
        {
            _language = NormalizeLanguage(language);
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new
            {
                type = "monaco.setLanguage",
                language = _language
            }).ConfigureAwait(false);
        }

        public async Task SetThemeAsync(string theme)
        {
            _theme = NormalizeTheme(theme);
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new
            {
                type = "monaco.setTheme",
                theme = _theme
            }).ConfigureAwait(false);
        }

        public void FocusEditor()
        {
            if (_disposed)
                return;

            _host.FocusHost();
            if (_host.IsInitialized)
                _ = _host.PostMessageAsync(new { type = "monaco.focus" });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _host.ReadyStateChanged -= HandleHostReadyStateChanged;
            _host.MessageReceived -= HandleHostMessageReceived;
            _host.Dispose();
            _workspace.Dispose();
        }

        private async Task InitializeCoreAsync()
        {
            await _workspace.PrepareAsync().ConfigureAwait(false);
            await _host.InitializeAsync().ConfigureAwait(false);
            await _host.LoadEntryPageAsync(_options.EntryPageRelativePath).ConfigureAwait(false);
        }

        private void HandleHostReadyStateChanged(object sender, WebComponentReadyStateChangedEventArgs e)
        {
            ReadyStateChanged?.Invoke(this, new MonacoEditorReadyStateChangedEventArgs(e.IsInitialized, e.IsReady));

            if (e.IsReady)
                _ = ReplayStateAsync();
        }

        private async Task ReplayStateAsync()
        {
            try
            {
                await _host.PostMessageAsync(new { type = "monaco.setLanguage", language = _language }).ConfigureAwait(false);
                await _host.PostMessageAsync(new { type = "monaco.setTheme", theme = _theme }).ConfigureAwait(false);
                await _host.PostMessageAsync(new { type = "monaco.setValue", value = _value }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new MonacoEditorErrorEventArgs("Failed to replay MonacoEditor state.", ex.Message));
            }
        }

        private void HandleHostMessageReceived(object sender, WebComponentMessageEventArgs e)
        {
            if (_disposed || string.IsNullOrWhiteSpace(e.RawMessage))
                return;

            try
            {
                using (var document = JsonDocument.Parse(e.RawMessage))
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                        return;

                    string messageType = ReadString(document.RootElement, "type");
                    switch (messageType)
                    {
                        case "monaco.contentChanged":
                            _value = ReadString(document.RootElement, "value");
                            ContentChanged?.Invoke(this, new MonacoEditorContentChangedEventArgs(_value));
                            break;

                        case "monaco.error":
                            ErrorOccurred?.Invoke(
                                this,
                                new MonacoEditorErrorEventArgs(
                                    ReadString(document.RootElement, "message"),
                                    ReadString(document.RootElement, "detail")));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new MonacoEditorErrorEventArgs("Failed to parse MonacoEditor message.", ex.Message));
            }
        }

        private static string NormalizeLanguage(string language)
        {
            return string.IsNullOrWhiteSpace(language) ? "plaintext" : language.Trim();
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";
        }

        private static string ReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return string.Empty;

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
