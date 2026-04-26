using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.DocumentEditor
{
    public sealed class DocumentEditorRuntime : IDisposable
    {
        private readonly IWebComponentHost _host;
        private readonly DocumentEditorWorkspace _workspace;
        private readonly DocumentEditorOptions _options;
        private Task _initializeTask;
        private bool _disposed;
        private string _html;
        private string _markdown;
        private string _documentJson;
        private string _theme;
        private string _languageCode;
        private DocumentEditorState _state;

        public DocumentEditorRuntime(IWebComponentHost host, DocumentEditorWorkspace workspace, DocumentEditorOptions options)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _options = (options ?? new DocumentEditorOptions()).Clone();
            _html = string.Empty;
            _markdown = string.Empty;
            _documentJson = _options.InitialDocumentJson ?? string.Empty;
            _theme = NormalizeTheme(_options.InitialTheme);
            _languageCode = NormalizeLanguageCode(_options.InitialLanguageCode);
            _state = new DocumentEditorState
            {
                IsReadOnly = _options.IsReadOnly,
            };

            _host.ReadyStateChanged += HandleHostReadyStateChanged;
            _host.MessageReceived += HandleHostMessageReceived;
        }

        public bool IsInitialized => _host.IsInitialized;

        public bool IsReady => _host.IsReady;

        public DocumentEditorState State => _state;

        public string Theme => _theme;

        public event EventHandler<DocumentEditorReadyStateChangedEventArgs> ReadyStateChanged;

        public event EventHandler<DocumentEditorContentChangedEventArgs> ContentChanged;

        public event EventHandler<DocumentEditorSelectionChangedEventArgs> SelectionChanged;

        public event EventHandler<DocumentEditorErrorEventArgs> ErrorOccurred;

        public event EventHandler<string> ThemeChanged;

        public event EventHandler<DocumentEditorNativeFileActionRequestedEventArgs> NativeFileActionRequested;

        public Task InitializeAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DocumentEditorRuntime));
            }

            if (_initializeTask == null)
            {
                _initializeTask = InitializeCoreAsync();
            }

            return _initializeTask;
        }

        public async Task SetHtmlAsync(string html)
        {
            _html = html ?? string.Empty;
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setHtml", html = _html }).ConfigureAwait(false);
        }

        public async Task<string> GetHtmlAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await RequestStateAsync().ConfigureAwait(false);
            return _html;
        }

        public async Task SetMarkdownAsync(string markdown)
        {
            _markdown = markdown ?? string.Empty;
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setMarkdown", markdown = _markdown }).ConfigureAwait(false);
        }

        public async Task<string> GetMarkdownAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await RequestStateAsync().ConfigureAwait(false);
            return _markdown;
        }

        public async Task SetDocumentJsonAsync(string documentJson)
        {
            _documentJson = documentJson ?? string.Empty;
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setDocumentJson", documentJson = _documentJson }).ConfigureAwait(false);
        }

        public async Task SetThemeAsync(string theme)
        {
            _theme = NormalizeTheme(theme);
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setTheme", theme = _theme }).ConfigureAwait(false);
        }

        public async Task SetLanguageAsync(string languageCode)
        {
            _languageCode = NormalizeLanguageCode(languageCode);
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setLanguage", languageCode = _languageCode }).ConfigureAwait(false);
        }

        public async Task<string> GetDocumentJsonAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await RequestStateAsync().ConfigureAwait(false);
            return _documentJson;
        }

        public async Task SetReadOnlyAsync(bool isReadOnly)
        {
            _state.IsReadOnly = isReadOnly;
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setReadOnly", isReadOnly }).ConfigureAwait(false);
        }

        public async Task SetToolbarAsync(DocumentEditorToolbarConfig toolbar)
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.setToolbarConfig", toolbar = CreateToolbarPayload(toolbar) }).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.clear" }).ConfigureAwait(false);
        }

        public void FocusEditor()
        {
            if (_disposed || !_host.IsInitialized)
            {
                return;
            }

            _ = _host.PostMessageAsync(new { type = "documentEditor.focus" });
        }

        public async Task ExecuteCommandAsync(DocumentEditorCommand command, string value = null)
        {
            await InitializeAsync().ConfigureAwait(false);
            await _host.PostMessageAsync(new { type = "documentEditor.executeCommand", command = ToMessageCommand(command), value = value ?? string.Empty }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

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

        private async Task RequestStateAsync()
        {
            if (_host.IsReady)
            {
                await _host.PostMessageAsync(new { type = "documentEditor.requestState" }).ConfigureAwait(false);
            }
        }

        private void HandleHostReadyStateChanged(object sender, WebComponentReadyStateChangedEventArgs e)
        {
            ReadyStateChanged?.Invoke(this, new DocumentEditorReadyStateChangedEventArgs(e.IsInitialized, e.IsReady));

            if (e.IsReady)
            {
                _ = ReplayStateAsync();
            }
        }

        private async Task ReplayStateAsync()
        {
            try
            {
                await _host.PostMessageAsync(new { type = "documentEditor.setReadOnly", isReadOnly = _state.IsReadOnly }).ConfigureAwait(false);
                await _host.PostMessageAsync(new { type = "documentEditor.setTheme", theme = _theme }).ConfigureAwait(false);
                await _host.PostMessageAsync(new { type = "documentEditor.setLanguage", languageCode = _languageCode }).ConfigureAwait(false);
                await _host.PostMessageAsync(new { type = "documentEditor.setToolbarConfig", toolbar = CreateToolbarPayload(_options.Toolbar) }).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(_documentJson))
                {
                    await _host.PostMessageAsync(new { type = "documentEditor.setDocumentJson", documentJson = _documentJson }).ConfigureAwait(false);
                }

                await RequestStateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new DocumentEditorErrorEventArgs("Failed to replay DocumentEditor state.", ex.Message));
            }
        }

        private void HandleHostMessageReceived(object sender, WebComponentMessageEventArgs e)
        {
            if (_disposed || string.IsNullOrWhiteSpace(e.RawMessage))
            {
                return;
            }

            try
            {
                using (var document = JsonDocument.Parse(e.RawMessage))
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return;
                    }

                    var root = document.RootElement;
                    string messageType = ReadString(root, "type");

                    if (messageType == "documentEditor.error")
                    {
                        ErrorOccurred?.Invoke(this, new DocumentEditorErrorEventArgs(ReadString(root, "message"), ReadString(root, "detail")));
                        return;
                    }

                    if (TryRaiseNativeFileAction(messageType, root))
                    {
                        return;
                    }

                    _html = ReadString(root, "html");
                    _markdown = ReadString(root, "markdown");
                    _documentJson = ReadString(root, "documentJson");
                    string nextTheme = NormalizeTheme(ReadString(root, "theme"));
                    var themeChanged = !string.Equals(_theme, nextTheme, StringComparison.OrdinalIgnoreCase);
                    _theme = nextTheme;
                    _state = new DocumentEditorState
                    {
                        IsReadOnly = ReadBoolean(root, "isReadOnly"),
                        CanUndo = ReadBoolean(root, "canUndo"),
                        CanRedo = ReadBoolean(root, "canRedo"),
                        IsDirty = ReadBoolean(root, "isDirty"),
                        IsEmpty = ReadBoolean(root, "isEmpty"),
                    };

                    if (messageType == "documentEditor.selectionChanged")
                    {
                        SelectionChanged?.Invoke(this, new DocumentEditorSelectionChangedEventArgs(
                            ReadNestedInt(root, "selection", "from"),
                            ReadNestedInt(root, "selection", "to"),
                            ReadNestedBoolean(root, "selection", "empty")));
                    }

                    if (messageType == "documentEditor.contentChanged" || messageType == "documentEditor.stateChanged")
                    {
                        ContentChanged?.Invoke(this, new DocumentEditorContentChangedEventArgs(_html, _markdown, _documentJson, _state));
                    }

                    if (themeChanged)
                    {
                        ThemeChanged?.Invoke(this, _theme);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new DocumentEditorErrorEventArgs("Failed to parse DocumentEditor message.", ex.Message));
            }
        }

        private bool TryRaiseNativeFileAction(string messageType, JsonElement root)
        {
            DocumentEditorNativeFileActionKind kind;
            if (messageType == "documentEditor.exportHtmlRequested")
            {
                kind = DocumentEditorNativeFileActionKind.ExportHtml;
            }
            else if (messageType == "documentEditor.exportMarkdownRequested")
            {
                kind = DocumentEditorNativeFileActionKind.ExportMarkdown;
            }
            else if (messageType == "documentEditor.saveJsonRequested")
            {
                kind = DocumentEditorNativeFileActionKind.SaveJson;
            }
            else if (messageType == "documentEditor.loadJsonRequested")
            {
                kind = DocumentEditorNativeFileActionKind.LoadJson;
            }
            else
            {
                return false;
            }

            NativeFileActionRequested?.Invoke(this, new DocumentEditorNativeFileActionRequestedEventArgs(
                kind,
                ReadString(root, "html"),
                ReadString(root, "markdown"),
                ReadString(root, "documentJson")));
            return true;
        }

        private static string ToMessageCommand(DocumentEditorCommand command)
        {
            switch (command)
            {
                case DocumentEditorCommand.Undo: return "undo";
                case DocumentEditorCommand.Redo: return "redo";
                case DocumentEditorCommand.Paragraph: return "paragraph";
                case DocumentEditorCommand.Heading1: return "heading1";
                case DocumentEditorCommand.Heading2: return "heading2";
                case DocumentEditorCommand.Heading3: return "heading3";
                case DocumentEditorCommand.Bold: return "bold";
                case DocumentEditorCommand.Italic: return "italic";
                case DocumentEditorCommand.Underline: return "underline";
                case DocumentEditorCommand.Strike: return "strike";
                case DocumentEditorCommand.InlineCode: return "inlineCode";
                case DocumentEditorCommand.BulletList: return "bulletList";
                case DocumentEditorCommand.OrderedList: return "orderedList";
                case DocumentEditorCommand.Blockquote: return "blockquote";
                case DocumentEditorCommand.HorizontalRule: return "horizontalRule";
                case DocumentEditorCommand.ClearFormatting: return "clearFormatting";
                case DocumentEditorCommand.TextColor: return "textColor";
                case DocumentEditorCommand.HighlightColor: return "highlightColor";
                case DocumentEditorCommand.FontFamily: return "fontFamily";
                case DocumentEditorCommand.FontSize: return "fontSize";
                case DocumentEditorCommand.LineHeight: return "lineHeight";
                case DocumentEditorCommand.AlignLeft: return "alignLeft";
                case DocumentEditorCommand.AlignCenter: return "alignCenter";
                case DocumentEditorCommand.AlignRight: return "alignRight";
                case DocumentEditorCommand.AlignJustify: return "alignJustify";
                case DocumentEditorCommand.Link: return "link";
                case DocumentEditorCommand.Image: return "image";
                case DocumentEditorCommand.InsertTable: return "insertTable";
                case DocumentEditorCommand.AddColumnBefore: return "addColumnBefore";
                case DocumentEditorCommand.AddColumnAfter: return "addColumnAfter";
                case DocumentEditorCommand.DeleteColumn: return "deleteColumn";
                case DocumentEditorCommand.AddRowBefore: return "addRowBefore";
                case DocumentEditorCommand.AddRowAfter: return "addRowAfter";
                case DocumentEditorCommand.DeleteRow: return "deleteRow";
                case DocumentEditorCommand.DeleteTable: return "deleteTable";
                case DocumentEditorCommand.MergeCells: return "mergeCells";
                case DocumentEditorCommand.SplitCell: return "splitCell";
                case DocumentEditorCommand.ToggleHeaderRow: return "toggleHeaderRow";
                case DocumentEditorCommand.ToggleHeaderColumn: return "toggleHeaderColumn";
                case DocumentEditorCommand.Focus: return "focus";
                case DocumentEditorCommand.Clear: return "clear";
                case DocumentEditorCommand.ExportHtml: return "exportHtml";
                case DocumentEditorCommand.ExportMarkdown: return "exportMarkdown";
                case DocumentEditorCommand.SaveJson: return "saveJson";
                case DocumentEditorCommand.LoadJson: return "loadJson";
                default: throw new InvalidOperationException("Unsupported DocumentEditor command: " + command);
            }
        }

        private static string ReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                return string.Empty;
            }

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static bool ReadBoolean(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            return false;
        }

        private static int ReadNestedInt(JsonElement root, string objectName, string propertyName)
        {
            if (!root.TryGetProperty(objectName, out var nested) || nested.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }

            if (!nested.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
            {
                return 0;
            }

            return value.GetInt32();
        }

        private static bool ReadNestedBoolean(JsonElement root, string objectName, string propertyName)
        {
            if (!root.TryGetProperty(objectName, out var nested) || nested.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!nested.TryGetProperty(propertyName, out var value))
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            return false;
        }

        private static string NormalizeTheme(string theme)
        {
            return string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(theme, "night", StringComparison.OrdinalIgnoreCase)
                ? "dark"
                : "light";
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.Equals(languageCode, "pl", StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";
        }

        private static object CreateToolbarPayload(DocumentEditorToolbarConfig toolbar)
        {
            var normalizedToolbar = (toolbar ?? DocumentEditorToolbarConfig.CreateDefault()).Clone();
            var items = normalizedToolbar.Items ?? new DocumentEditorToolbarItem[0];
            var payloadItems = new object[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                payloadItems[i] = new
                {
                    command = ToMessageCommand(items[i].Command),
                    isVisible = items[i].IsVisible,
                    order = items[i].Order,
                };
            }

            return new
            {
                items = payloadItems,
            };
        }
    }
}
