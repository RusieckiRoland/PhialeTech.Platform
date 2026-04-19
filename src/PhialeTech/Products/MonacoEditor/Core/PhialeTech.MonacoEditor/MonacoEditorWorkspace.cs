using PhialeTech.MonacoEditor.Abstractions;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhialeTech.MonacoEditor
{
    public sealed class MonacoEditorWorkspace : IDisposable
    {
        private readonly MonacoEditorOptions _options;
        private readonly string _workspaceRootPath;
        private readonly string _editorRootPath;
        private readonly string _monacoRootPath;
        private readonly string _sourceEditorAssetRootPath;
        private readonly string _sourceMonacoAssetRootPath;
        private bool _prepared;

        public MonacoEditorWorkspace(MonacoEditorOptions options)
        {
            _options = (options ?? new MonacoEditorOptions()).Clone();
            _workspaceRootPath = Path.Combine(Path.GetTempPath(), "PhialeTech", "MonacoEditor", Guid.NewGuid().ToString("N"));
            _editorRootPath = Path.Combine(_workspaceRootPath, _options.AssetRootRelativePath ?? "MonacoEditor");
            _monacoRootPath = Path.Combine(_workspaceRootPath, _options.MonacoAssetRootRelativePath ?? "Monaco");
            _sourceEditorAssetRootPath = WebAssetLocationResolver.ResolveAssetPath(_options.AssetRootRelativePath ?? "MonacoEditor");
            _sourceMonacoAssetRootPath = WebAssetLocationResolver.ResolveAssetPath(_options.MonacoAssetRootRelativePath ?? "Monaco");
        }

        public string WorkspaceRootPath => _workspaceRootPath;

        public async Task PrepareAsync()
        {
            if (_prepared)
                return;

            if (!Directory.Exists(_sourceEditorAssetRootPath))
                throw new DirectoryNotFoundException("MonacoEditor assets were not found at: " + _sourceEditorAssetRootPath);

            if (!Directory.Exists(_sourceMonacoAssetRootPath))
                throw new DirectoryNotFoundException("Shared Monaco assets were not found at: " + _sourceMonacoAssetRootPath);

            Directory.CreateDirectory(_workspaceRootPath);
            CopyDirectory(_sourceEditorAssetRootPath, _editorRootPath);
            CopyDirectory(_sourceMonacoAssetRootPath, _monacoRootPath);
            await WriteBootstrapConfigAsync().ConfigureAwait(false);
            _prepared = true;
        }

        public void Dispose()
        {
            TryDeleteDirectory(_workspaceRootPath);
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

        private async Task WriteBootstrapConfigAsync()
        {
            Directory.CreateDirectory(_editorRootPath);

            string bootstrapConfigPath = Path.Combine(_editorRootPath, "monaco-editor.bootstrap.json");
            var payload = new
            {
                theme = string.Equals(_options.InitialTheme, "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light",
                language = string.IsNullOrWhiteSpace(_options.InitialLanguage) ? "plaintext" : _options.InitialLanguage,
                value = _options.InitialValue ?? string.Empty,
            };

            using (var stream = File.Create(bootstrapConfigPath))
            {
                await JsonSerializer.SerializeAsync(stream, payload).ConfigureAwait(false);
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return;

            try
            {
                if (Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
