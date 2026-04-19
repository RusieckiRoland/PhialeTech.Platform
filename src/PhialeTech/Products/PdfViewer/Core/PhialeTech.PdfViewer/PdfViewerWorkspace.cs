using PhialeTech.PdfViewer.Abstractions;
using PhialeTech.WebHost;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PhialeTech.PdfViewer
{
    public sealed class PdfViewerWorkspace : IDisposable
    {
        private readonly PdfViewerOptions _options;
        private readonly string _workspaceRootPath;
        private readonly string _viewerRootPath;
        private readonly string _documentsPath;
        private readonly string _sourceAssetRootPath;
        private bool _prepared;
        private string _currentManagedDocumentPath;

        public PdfViewerWorkspace(PdfViewerOptions options)
        {
            _options = (options ?? new PdfViewerOptions()).Clone();
            _workspaceRootPath = Path.Combine(Path.GetTempPath(), "PhialeTech", "PdfViewer", Guid.NewGuid().ToString("N"));
            _viewerRootPath = Path.Combine(_workspaceRootPath, _options.AssetRootRelativePath ?? "PdfViewer");
            _documentsPath = Path.Combine(_viewerRootPath, "documents");
            _sourceAssetRootPath = WebAssetLocationResolver.ResolveAssetPath(_options.AssetRootRelativePath ?? "PdfViewer");
        }

        public string WorkspaceRootPath => _workspaceRootPath;

        public string ViewerRootPath => _viewerRootPath;

        public async Task PrepareAsync()
        {
            if (_prepared)
                return;

            if (!Directory.Exists(_sourceAssetRootPath))
                throw new DirectoryNotFoundException("PdfViewer assets were not found at: " + _sourceAssetRootPath);

            Directory.CreateDirectory(_workspaceRootPath);
            CopyDirectory(_sourceAssetRootPath, _viewerRootPath);
            Directory.CreateDirectory(_documentsPath);
            _prepared = true;

            await Task.CompletedTask;
        }

        public async Task<NormalizedPdfSource> NormalizeAsync(PdfDocumentSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            await PrepareAsync().ConfigureAwait(false);

            if (source.Uri != null)
            {
                if (source.Uri.IsFile)
                    return await CopyFileIntoWorkspaceAsync(source.Uri.LocalPath, source.FileName).ConfigureAwait(false);

                return new NormalizedPdfSource(source.Uri.AbsoluteUri, source.FileName, false, null);
            }

            if (!string.IsNullOrWhiteSpace(source.FilePath))
                return await CopyFileIntoWorkspaceAsync(source.FilePath, source.FileName).ConfigureAwait(false);

            if (source.Bytes != null)
                return await WriteBytesIntoWorkspaceAsync(source.Bytes, source.FileName).ConfigureAwait(false);

            if (source.Stream != null)
                return await CopyStreamIntoWorkspaceAsync(source.Stream, source.FileName, source.LeaveStreamOpen).ConfigureAwait(false);

            throw new InvalidOperationException("PdfDocumentSource does not contain any supported source.");
        }

        public void Dispose()
        {
            TryDeleteFile(_currentManagedDocumentPath);
            TryDeleteDirectory(_workspaceRootPath);
        }

        private async Task<NormalizedPdfSource> CopyFileIntoWorkspaceAsync(string filePath, string fileName)
        {
            string sourceFilePath = Path.GetFullPath(filePath ?? string.Empty);
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("PDF source file was not found.", sourceFilePath);

            string destinationPath = BuildManagedDocumentPath(fileName);
            File.Copy(sourceFilePath, destinationPath, true);
            ReplaceManagedDocument(destinationPath);

            return await Task.FromResult(CreateManagedSource(destinationPath, fileName)).ConfigureAwait(false);
        }

        private async Task<NormalizedPdfSource> WriteBytesIntoWorkspaceAsync(byte[] bytes, string fileName)
        {
            string destinationPath = BuildManagedDocumentPath(fileName);
            using (var fileStream = File.Create(destinationPath))
            {
                await fileStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
            ReplaceManagedDocument(destinationPath);
            return CreateManagedSource(destinationPath, fileName);
        }

        private async Task<NormalizedPdfSource> CopyStreamIntoWorkspaceAsync(Stream stream, string fileName, bool leaveOpen)
        {
            string destinationPath = BuildManagedDocumentPath(fileName);
            using (var fileStream = File.Create(destinationPath))
            {
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            if (!leaveOpen)
                stream.Dispose();

            ReplaceManagedDocument(destinationPath);
            return CreateManagedSource(destinationPath, fileName);
        }

        private NormalizedPdfSource CreateManagedSource(string filePath, string fileName)
        {
            string safeName = Path.GetFileName(filePath);
            string relativeSource = "documents/" + Uri.EscapeDataString(safeName.Replace('\\', '/'));
            return new NormalizedPdfSource(relativeSource, fileName, true, filePath);
        }

        private string BuildManagedDocumentPath(string fileName)
        {
            string safeFileName = string.IsNullOrWhiteSpace(fileName)
                ? "document.pdf"
                : Path.GetFileName(fileName);

            string uniqueFileName = Guid.NewGuid().ToString("N") + "-" + safeFileName;
            return Path.Combine(_documentsPath, uniqueFileName);
        }

        private void ReplaceManagedDocument(string newManagedDocumentPath)
        {
            string previous = _currentManagedDocumentPath;
            _currentManagedDocumentPath = newManagedDocumentPath;

            if (!string.IsNullOrWhiteSpace(previous) &&
                !string.Equals(previous, newManagedDocumentPath, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteFile(previous);
            }
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

        private static void TryDeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // Best-effort cleanup.
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

        public sealed class NormalizedPdfSource
        {
            public NormalizedPdfSource(string viewerSource, string displayName, bool isManagedTempFile, string managedFilePath)
            {
                ViewerSource = viewerSource ?? string.Empty;
                DisplayName = displayName ?? string.Empty;
                IsManagedTempFile = isManagedTempFile;
                ManagedFilePath = managedFilePath ?? string.Empty;
            }

            public string ViewerSource { get; }

            public string DisplayName { get; }

            public bool IsManagedTempFile { get; }

            public string ManagedFilePath { get; }
        }
    }
}
