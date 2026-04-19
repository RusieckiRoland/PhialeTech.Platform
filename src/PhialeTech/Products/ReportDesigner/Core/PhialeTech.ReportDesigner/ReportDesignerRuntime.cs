using PhialeTech.ReportDesigner.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.ReportDesigner
{
    public sealed class ReportDesignerRuntime : IDisposable
    {
        private readonly IWebComponentHost _host;
        private readonly ReportDesignerWorkspace _workspace;
        private readonly ReportDesignerOptions _options;
        private readonly Dictionary<string, TaskCompletionSource<ReportDefinition>> _pendingDefinitionRequests =
            new Dictionary<string, TaskCompletionSource<ReportDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private Task _initializeTask;
        private bool _disposed;
        private int _requestSequence;
        private ReportDefinition _latestDefinition = new ReportDefinition();
        private ReportDataSchema _latestSchema = new ReportDataSchema();
        private string _sampleDataJson = "{}";
        private string _reportDataJson = "{}";
        private ReportDesignerMode _mode = ReportDesignerMode.Design;
        private string _locale;
        private string _theme;

        public ReportDesignerRuntime(
            IWebComponentHost host,
            ReportDesignerWorkspace workspace,
            ReportDesignerOptions options)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _options = (options ?? new ReportDesignerOptions()).Clone();
            _locale = ReportDesignerShellTextCatalog.NormalizeLocale(_options.InitialLocale);
            _theme = NormalizeTheme(_options.InitialTheme);

            _host.ReadyStateChanged += HandleHostReadyStateChanged;
            _host.MessageReceived += HandleHostMessageReceived;
        }

        public bool IsInitialized => _host.IsInitialized;

        public bool IsReady => _host.IsReady;

        public ReportDesignerMode Mode => _mode;

        public string Locale => _locale;

        public string Theme => _theme;

        public event EventHandler<ReportDesignerReadyStateChangedEventArgs> ReadyStateChanged;

        public event EventHandler<ReportDefinitionChangedEventArgs> DefinitionChanged;

        public event EventHandler<ReportPreviewReadyEventArgs> PreviewReady;

        public event EventHandler<ReportDesignerModeChangedEventArgs> ModeChanged;

        public event EventHandler<ReportDesignerErrorEventArgs> ErrorOccurred;

        public Task InitializeAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ReportDesignerRuntime));

            if (_initializeTask == null)
                _initializeTask = InitializeCoreAsync();

            return _initializeTask;
        }

        public async Task SetModeAsync(ReportDesignerMode mode)
        {
            await InitializeAsync().ConfigureAwait(false);
            _mode = mode;
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.setMode",
                mode = MapMode(mode)
            }).ConfigureAwait(false);
        }

        public async Task SetLocaleAsync(string locale)
        {
            await InitializeAsync().ConfigureAwait(false);
            _locale = ReportDesignerShellTextCatalog.NormalizeLocale(locale);
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.setLocale",
                locale = _locale
            }).ConfigureAwait(false);
        }

        public async Task SetThemeAsync(string theme)
        {
            await InitializeAsync().ConfigureAwait(false);
            _theme = NormalizeTheme(theme);
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.setTheme",
                theme = _theme
            }).ConfigureAwait(false);
        }

        public async Task LoadDefinitionAsync(ReportDefinition definition)
        {
            await InitializeAsync().ConfigureAwait(false);
            _latestDefinition = definition ?? new ReportDefinition();
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.loadDefinition",
                definition = _latestDefinition
            }).ConfigureAwait(false);
        }

        public async Task<ReportDefinition> GetDefinitionAsync()
        {
            await InitializeAsync().ConfigureAwait(false);

            string requestId = "definition-" + (++_requestSequence).ToString();
            var tcs = new TaskCompletionSource<ReportDefinition>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingDefinitionRequests[requestId] = tcs;

            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.getDefinition",
                requestId = requestId
            }).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task SetDataSchemaAsync(ReportDataSchema schema)
        {
            await InitializeAsync().ConfigureAwait(false);
            _latestSchema = schema ?? new ReportDataSchema();
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.setDataSchema",
                schema = _latestSchema
            }).ConfigureAwait(false);
        }

        public async Task SetSampleDataAsync(string json)
        {
            await InitializeAsync().ConfigureAwait(false);
            _sampleDataJson = NormalizeJson(json);
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.setSampleData",
                json = _sampleDataJson
            }).ConfigureAwait(false);
        }

        public async Task SetReportDataAsync(string json)
        {
            await InitializeAsync().ConfigureAwait(false);
            _reportDataJson = NormalizeJson(json);
            await _host.PostMessageAsync(new
            {
                type = "reportDesigner.setReportData",
                json = _reportDataJson
            }).ConfigureAwait(false);
        }

        public async Task RefreshPreviewAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "reportDesigner.refreshPreview" }).ConfigureAwait(false);
        }

        public async Task PrintAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "reportDesigner.print" }).ConfigureAwait(false);
        }

        public void FocusDesigner()
        {
            if (_disposed)
                return;

            _host.FocusHost();
            if (_host.IsInitialized)
            {
                _ = _host.PostMessageAsync(new { type = "reportDesigner.focus" });
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _host.ReadyStateChanged -= HandleHostReadyStateChanged;
            _host.MessageReceived -= HandleHostMessageReceived;

            foreach (var entry in _pendingDefinitionRequests.Values)
            {
                entry.TrySetCanceled();
            }

            _pendingDefinitionRequests.Clear();
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
            ReadyStateChanged?.Invoke(this, new ReportDesignerReadyStateChangedEventArgs(e.IsInitialized, e.IsReady));

            if (e.IsReady)
            {
                _ = ReplayStateAsync();
            }
        }

        private async Task ReplayStateAsync()
        {
            try
            {
                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.setLocale",
                    locale = _locale
                }).ConfigureAwait(false);

                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.setTheme",
                    theme = _theme
                }).ConfigureAwait(false);

                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.loadDefinition",
                    definition = _latestDefinition
                }).ConfigureAwait(false);

                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.setDataSchema",
                    schema = _latestSchema
                }).ConfigureAwait(false);

                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.setSampleData",
                    json = _sampleDataJson
                }).ConfigureAwait(false);

                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.setReportData",
                    json = _reportDataJson
                }).ConfigureAwait(false);

                await _host.PostMessageAsync(new
                {
                    type = "reportDesigner.setMode",
                    mode = MapMode(_mode)
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ReportDesignerErrorEventArgs("Failed to replay ReportDesigner state.", ex.Message));
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
                        case "reportDesigner.definitionChanged":
                            _latestDefinition = ReadDefinition(document.RootElement, "definition");
                            DefinitionChanged?.Invoke(this, new ReportDefinitionChangedEventArgs(_latestDefinition));
                            break;

                        case "reportDesigner.definitionSnapshot":
                            string requestId = ReadString(document.RootElement, "requestId");
                            ReportDefinition definition = ReadDefinition(document.RootElement, "definition");
                            _latestDefinition = definition;

                            if (!string.IsNullOrWhiteSpace(requestId) &&
                                _pendingDefinitionRequests.TryGetValue(requestId, out var tcs))
                            {
                                _pendingDefinitionRequests.Remove(requestId);
                                tcs.TrySetResult(definition);
                            }
                            break;

                        case "reportDesigner.previewReady":
                            PreviewReady?.Invoke(
                                this,
                                new ReportPreviewReadyEventArgs(
                                    ReadInt(document.RootElement, "pageCount"),
                                    ReadBool(document.RootElement, "usedSampleData")));
                            break;

                        case "reportDesigner.modeChanged":
                            _mode = ParseMode(ReadString(document.RootElement, "mode"));
                            ModeChanged?.Invoke(this, new ReportDesignerModeChangedEventArgs(_mode));
                            break;

                        case "reportDesigner.error":
                            ErrorOccurred?.Invoke(
                                this,
                                new ReportDesignerErrorEventArgs(
                                    ReadString(document.RootElement, "message"),
                                    ReadString(document.RootElement, "detail")));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new ReportDesignerErrorEventArgs("Failed to parse ReportDesigner message.", ex.Message));
            }
        }

        private ReportDefinition ReadDefinition(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var definitionElement) ||
                definitionElement.ValueKind != JsonValueKind.Object)
            {
                return new ReportDefinition();
            }

            try
            {
                return JsonSerializer.Deserialize<ReportDefinition>(definitionElement.GetRawText(), _jsonOptions) ?? new ReportDefinition();
            }
            catch
            {
                return new ReportDefinition();
            }
        }

        private static string NormalizeJson(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? "{}" : json;
        }

        private static string MapMode(ReportDesignerMode mode)
        {
            return mode == ReportDesignerMode.Preview ? "preview" : "design";
        }

        private static ReportDesignerMode ParseMode(string rawMode)
        {
            return string.Equals(rawMode, "preview", StringComparison.OrdinalIgnoreCase)
                ? ReportDesignerMode.Preview
                : ReportDesignerMode.Design;
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
                ? "dark"
                : "light";
        }

        private static string ReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return string.Empty;

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static int ReadInt(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return 0;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                return intValue;

            return 0;
        }

        private static bool ReadBool(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
                return false;

            if (value.ValueKind == JsonValueKind.True)
                return true;

            if (value.ValueKind == JsonValueKind.False)
                return false;

            return false;
        }
    }
}
