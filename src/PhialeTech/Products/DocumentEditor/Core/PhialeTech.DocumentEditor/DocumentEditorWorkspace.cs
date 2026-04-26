using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.DocumentEditor
{
    public sealed class DocumentEditorWorkspace : IDisposable
    {
        private readonly DocumentEditorOptions _options;
        private readonly string _workspaceRootPath;
        private readonly string _editorRootPath;
        private readonly string _sourceEditorAssetRootPath;
        private bool _prepared;

        public DocumentEditorWorkspace(DocumentEditorOptions options)
        {
            _options = (options ?? new DocumentEditorOptions()).Clone();
            _workspaceRootPath = Path.Combine(Path.GetTempPath(), "PhialeTech", "DocumentEditor", Guid.NewGuid().ToString("N"));
            _editorRootPath = Path.Combine(_workspaceRootPath, _options.AssetRootRelativePath ?? "DocumentEditor");
            _sourceEditorAssetRootPath = WebAssetLocationResolver.ResolveAssetPath(_options.AssetRootRelativePath ?? "DocumentEditor");
        }

        public string WorkspaceRootPath => _workspaceRootPath;

        public async Task PrepareAsync()
        {
            if (_prepared)
            {
                return;
            }

            if (!Directory.Exists(_sourceEditorAssetRootPath))
            {
                throw new DirectoryNotFoundException("DocumentEditor assets were not found at: " + _sourceEditorAssetRootPath);
            }

            Directory.CreateDirectory(_workspaceRootPath);
            CopyDirectory(_sourceEditorAssetRootPath, _editorRootPath);
            await WriteBootstrapConfigAsync().ConfigureAwait(false);
            _prepared = true;
        }

        public void Dispose()
        {
            TryDeleteDirectory(_workspaceRootPath);
        }

        private async Task WriteBootstrapConfigAsync()
        {
            Directory.CreateDirectory(_editorRootPath);

            string bootstrapConfigPath = Path.Combine(_editorRootPath, "document-editor.bootstrap.json");
            var payload = new
            {
                isReadOnly = _options.IsReadOnly,
                theme = _options.InitialTheme,
                languageCode = NormalizeLanguageCode(_options.InitialLanguageCode),
                placeholder = _options.Placeholder ?? string.Empty,
                documentJson = _options.InitialDocumentJson ?? string.Empty,
                toolbar = CreateToolbarPayload(_options.Toolbar),
            };

            using (var stream = File.Create(bootstrapConfigPath))
            {
                await JsonSerializer.SerializeAsync(stream, payload).ConfigureAwait(false);
            }
        }

        private static object CreateToolbarPayload(DocumentEditorToolbarConfig toolbar)
        {
            var normalizedToolbar = (toolbar ?? DocumentEditorToolbarConfig.CreateDefault()).Clone();
            return new
            {
                items = (normalizedToolbar.Items ?? new DocumentEditorToolbarItem[0])
                    .Select(item => new
                    {
                        command = ToMessageCommand(item.Command),
                        isVisible = item.IsVisible,
                        order = item.Order,
                    })
                    .ToArray()
            };
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

        private static string NormalizeLanguageCode(string languageCode)
        {
            return string.Equals(languageCode, "pl", StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
            }

            foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                string destinationPath = Path.Combine(destinationDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationDirectory);
                File.Copy(file, destinationPath, true);
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
