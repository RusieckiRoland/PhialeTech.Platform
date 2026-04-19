using PhialeTech.ReportDesigner.Abstractions;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PhialeTech.ReportDesigner
{
    public sealed class ReportDesignerWorkspace : IDisposable
    {
        private readonly ReportDesignerOptions _options;
        private readonly string _workspaceRootPath;
        private readonly string _designerRootPath;
        private readonly string _sourceAssetRootPath;
        private bool _prepared;

        public ReportDesignerWorkspace(ReportDesignerOptions options)
        {
            _options = (options ?? new ReportDesignerOptions()).Clone();
            _workspaceRootPath = Path.Combine(Path.GetTempPath(), "PhialeTech", "ReportDesigner", Guid.NewGuid().ToString("N"));
            _designerRootPath = Path.Combine(_workspaceRootPath, _options.AssetRootRelativePath ?? "ReportDesigner");
            _sourceAssetRootPath = WebAssetLocationResolver.ResolveAssetPath(_options.AssetRootRelativePath ?? "ReportDesigner");
        }

        public string WorkspaceRootPath => _workspaceRootPath;

        public async Task PrepareAsync()
        {
            if (_prepared)
                return;

            if (!Directory.Exists(_sourceAssetRootPath))
                throw new DirectoryNotFoundException("ReportDesigner assets were not found at: " + _sourceAssetRootPath);

            Directory.CreateDirectory(_workspaceRootPath);
            CopyDirectory(_sourceAssetRootPath, _designerRootPath);
            _prepared = true;

            await Task.CompletedTask.ConfigureAwait(false);
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
