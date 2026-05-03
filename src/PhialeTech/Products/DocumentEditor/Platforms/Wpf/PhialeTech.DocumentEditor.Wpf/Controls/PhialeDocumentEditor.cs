using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.WebHost.Abstractions.Ui.Web;
using PhialeTech.WebHost.Wpf;
using PhialeTech.WebHost.Wpf.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PhialeTech.DocumentEditor.Wpf.Controls
{
    public sealed class PhialeDocumentEditor : OverlayableWebComponentControlBase, IDocumentEditor
    {
        private static readonly Thickness HostInset = new Thickness(6);
        private readonly DocumentEditorOptions _options;
        private readonly DocumentEditorWorkspace _workspace;
        private readonly IWebComponentHost _host;
        private readonly DocumentEditorRuntime _runtime;
        private DocumentEditorOverlayMode _overlayMode;
        private bool _isOverlayOpen;
        private bool _disposed;

        public PhialeDocumentEditor()
            : this(new WpfWebComponentHostFactory(), new DocumentEditorOptions())
        {
        }

        public PhialeDocumentEditor(IWebComponentHostFactory hostFactory, DocumentEditorOptions options = null)
        {
            if (hostFactory == null)
            {
                throw new ArgumentNullException(nameof(hostFactory));
            }

            _options = (options ?? new DocumentEditorOptions()).Clone();
            _workspace = new DocumentEditorWorkspace(_options);
            _host = hostFactory.CreateHost(new WebComponentHostOptions
            {
                LocalContentRootPath = _workspace.WorkspaceRootPath,
                JavaScriptReadyMessageType = _options.ReadyMessageType,
                VirtualHostName = _options.VirtualHostName,
                QueueMessagesUntilReady = true
            });

            var hostElement = _host as UIElement;
            if (hostElement == null)
            {
                throw new InvalidOperationException("The supplied WPF web host factory did not return a WPF UI element.");
            }

            _runtime = new DocumentEditorRuntime(_host, _workspace, _options);
            _overlayMode = _options.OverlayMode;
            _runtime.NativeFileActionRequested += HandleNativeFileActionRequested;
            _host.MessageReceived += HandleHostMessageReceived;

            InitializeOverlaySurface(hostElement, HostInset, new Thickness(12), new CornerRadius(14));
            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        public DocumentEditorOptions Options => _options;

        public new bool IsInitialized => _runtime.IsInitialized;

        public bool IsReady => _runtime.IsReady;

        public DocumentEditorState State => _runtime.State;

        public string Theme => _runtime.Theme;

        public event EventHandler<DocumentEditorReadyStateChangedEventArgs> ReadyStateChanged
        {
            add { _runtime.ReadyStateChanged += value; }
            remove { _runtime.ReadyStateChanged -= value; }
        }

        public event EventHandler<DocumentEditorContentChangedEventArgs> ContentChanged
        {
            add { _runtime.ContentChanged += value; }
            remove { _runtime.ContentChanged -= value; }
        }

        public event EventHandler<DocumentEditorSelectionChangedEventArgs> SelectionChanged
        {
            add { _runtime.SelectionChanged += value; }
            remove { _runtime.SelectionChanged -= value; }
        }

        public event EventHandler<DocumentEditorErrorEventArgs> ErrorOccurred
        {
            add { _runtime.ErrorOccurred += value; }
            remove { _runtime.ErrorOccurred -= value; }
        }

        public event EventHandler<string> ThemeChanged
        {
            add { _runtime.ThemeChanged += value; }
            remove { _runtime.ThemeChanged -= value; }
        }

        public Task InitializeAsync() => _runtime.InitializeAsync();

        public Task SetHtmlAsync(string html) => _runtime.SetHtmlAsync(html);

        public Task<string> GetHtmlAsync() => _runtime.GetHtmlAsync();

        public Task SetMarkdownAsync(string markdown) => _runtime.SetMarkdownAsync(markdown);

        public Task<string> GetMarkdownAsync() => _runtime.GetMarkdownAsync();

        public Task SetDocumentJsonAsync(string documentJson) => _runtime.SetDocumentJsonAsync(documentJson);

        public Task<string> GetDocumentJsonAsync() => _runtime.GetDocumentJsonAsync();

        public Task SetThemeAsync(string theme) => _runtime.SetThemeAsync(theme);

        public Task SetLanguageAsync(string languageCode) => _runtime.SetLanguageAsync(languageCode);

        public Task SetOverlayModeAsync(DocumentEditorOverlayMode overlayMode)
        {
            _overlayMode = overlayMode;
            if (_overlayMode == DocumentEditorOverlayMode.Disabled && _isOverlayOpen)
            {
                SetOverlayOpen(false, true);
            }

            return _runtime.SetOverlayModeAsync(overlayMode);
        }

        public Task SetReadOnlyAsync(bool isReadOnly) => _runtime.SetReadOnlyAsync(isReadOnly);

        public Task SetToolbarAsync(DocumentEditorToolbarConfig toolbar) => _runtime.SetToolbarAsync(toolbar);

        public void FocusEditor() => _runtime.FocusEditor();

        public Task ExecuteCommandAsync(DocumentEditorCommand command, string value = null) => _runtime.ExecuteCommandAsync(command, value);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Loaded -= HandleLoaded;
            Unloaded -= HandleUnloaded;
            _runtime.NativeFileActionRequested -= HandleNativeFileActionRequested;
            _host.MessageReceived -= HandleHostMessageReceived;
            SetOverlayOpen(false, false);
            _runtime.Dispose();
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            _ = _runtime.InitializeAsync();
            if (_isOverlayOpen)
            {
                ApplyOverlayState(true);
            }
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isOverlayOpen)
            {
                ApplyOverlayState(false);
            }
        }

        private async void HandleNativeFileActionRequested(object sender, DocumentEditorNativeFileActionRequestedEventArgs e)
        {
            try
            {
                switch (e.Kind)
                {
                    case DocumentEditorNativeFileActionKind.ExportHtml:
                        SaveTextWithDesktopDialog("Export HTML", "HTML document (*.html)|*.html|All files (*.*)|*.*", "document-editor.html", e.Html);
                        return;
                    case DocumentEditorNativeFileActionKind.ExportMarkdown:
                        SaveTextWithDesktopDialog("Export Markdown", "Markdown document (*.md)|*.md|All files (*.*)|*.*", "document-editor.md", e.Markdown);
                        return;
                    case DocumentEditorNativeFileActionKind.SaveJson:
                        SaveTextWithDesktopDialog("Save Document JSON", "JSON document (*.json)|*.json|All files (*.*)|*.*", "document-editor.json", e.DocumentJson);
                        return;
                    case DocumentEditorNativeFileActionKind.LoadJson:
                        await LoadJsonWithDesktopDialogAsync().ConfigureAwait(true);
                        return;
                    default:
                        throw new InvalidOperationException("Unsupported DocumentEditor native file action: " + e.Kind);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DocumentEditor file operation failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SaveTextWithDesktopDialog(string title, string filter, string fileName, string content)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = fileName,
                AddExtension = true,
                OverwritePrompt = true,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            File.WriteAllText(dialog.FileName, content ?? string.Empty, Encoding.UTF8);
        }

        private async Task LoadJsonWithDesktopDialogAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Load Document JSON",
                Filter = "JSON document (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string documentJson = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            await SetDocumentJsonAsync(documentJson).ConfigureAwait(true);
        }

        private void HandleHostMessageReceived(object sender, WebComponentMessageEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.MessageType) || !string.Equals(e.MessageType, "documentEditor.toggleOverlay", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_overlayMode == DocumentEditorOverlayMode.Disabled)
            {
                return;
            }

            bool isOpen = ReadOverlayState(e.RawMessage);
            Dispatcher.BeginInvoke(new Action(() => SetOverlayOpen(isOpen, true)));
        }

        protected override void OnOverlayBackdropMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SetOverlayOpen(false, true);
        }

        private void SetOverlayOpen(bool isOpen, bool notifyWeb)
        {
            if (isOpen && _overlayMode == DocumentEditorOverlayMode.Disabled)
            {
                return;
            }

            _isOverlayOpen = isOpen;
            if (IsLoaded)
            {
                ApplyOverlayState(isOpen);
            }

            if (notifyWeb && _host.IsInitialized)
            {
                _ = _host.PostMessageAsync(new { type = "documentEditor.setOverlay", isOpen });
            }
        }

        private void ApplyOverlayState(bool isOpen)
        {
            if (isOpen)
            {
                if (_overlayMode == DocumentEditorOverlayMode.Container)
                {
                    OpenOverlayInNearestScope();
                    _host.FocusHost();
                    return;
                }

                if (_overlayMode == DocumentEditorOverlayMode.Window && OpenOverlayWindow())
                {
                    _host.FocusHost();
                    return;
                }

                return;
            }

            CloseOverlaySurface();
        }

        private static bool ReadOverlayState(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return false;
            }

            try
            {
                using (var document = JsonDocument.Parse(rawMessage))
                {
                    if (document.RootElement.ValueKind == JsonValueKind.Object &&
                        document.RootElement.TryGetProperty("isOpen", out var isOpen) &&
                        (isOpen.ValueKind == JsonValueKind.True || isOpen.ValueKind == JsonValueKind.False))
                    {
                        return isOpen.GetBoolean();
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
