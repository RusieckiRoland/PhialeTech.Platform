using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class PdfViewerPresentationArchitectureTests
    {
        [Test]
        public void PdfViewer_UsesWebToolbar_ThemeMessaging_AndThinPlatformHosts()
        {
            var abstractions = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "PdfViewer", "Abstractions", "PhialeTech.PdfViewer.Abstractions", "IPdfViewer.cs"));
            var options = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "PdfViewer", "Abstractions", "PhialeTech.PdfViewer.Abstractions", "PdfViewerOptions.cs"));
            var runtime = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "PdfViewer", "Core", "PhialeTech.PdfViewer", "PdfViewerRuntime.cs"));
            var html = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.WebAssets", "Assets", "PdfViewer", "index.html"));
            var script = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Shared", "PhialeTech.WebAssets", "Assets", "PdfViewer", "phiale-pdf-host.js"));
            var wpf = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "PdfViewer", "Platforms", "Wpf", "PhialeTech.PdfViewer.Wpf", "Controls", "PhialePdfViewer.cs"));
            var avalonia = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "PdfViewer", "Platforms", "Avalonia", "PhialeTech.PdfViewer.Avalonia", "Controls", "PhialePdfViewer.cs"));
            var winui = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "PhialeTech", "Products", "PdfViewer", "Platforms", "WinUI3", "PhialeTech.PdfViewer.WinUI", "Controls", "PhialePdfViewer.cs"));
            var showcase = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "PdfViewerShowcaseView.cs"));
            var mainWindow = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "MainWindow.xaml.cs"));

            Assert.That(abstractions, Does.Contain("Task SetThemeAsync(string theme);"));
            Assert.That(options, Does.Contain("public string InitialTheme { get; set; } = \"light\";"));
            Assert.That(runtime, Does.Contain("public string Theme => _theme;"));
            Assert.That(runtime, Does.Contain("pdf.setTheme"));
            Assert.That(runtime, Does.Contain("NormalizeTheme"));
            Assert.That(html, Does.Contain("id=\"toolbar\""));
            Assert.That(html, Does.Contain("id=\"viewerShell\""));
            Assert.That(html, Does.Contain("id=\"pageNumberInput\""));
            Assert.That(html, Does.Contain("id=\"zoomSelect\""));
            Assert.That(html, Does.Contain("data-theme=\"dark\""));
            Assert.That(html, Does.Contain(".pdf-toolbar"));
            Assert.That(html, Does.Contain(".pdf-toolbar-button"));
            Assert.That(html, Does.Not.Contain("@media (max-width: 720px)"));
            Assert.That(html, Does.Contain("overflow: visible;"));
            Assert.That(html, Does.Contain("width: 16px;"));
            Assert.That(html, Does.Contain("height: 16px;"));
            Assert.That(script, Does.Contain("const ICONS ="));
            Assert.That(script, Does.Contain("applyTheme("));
            Assert.That(script, Does.Contain("case \"pdf.setTheme\":"));
            Assert.That(script, Does.Contain("case \"pdf.setPage\":"));
            Assert.That(script, Does.Contain("case \"pdf.setZoom\":"));
            Assert.That(script, Does.Contain("type: \"pdf.download\""));
            Assert.That(script, Does.Contain("case \"pdf.rotateClockwise\":"));
            Assert.That(script, Does.Contain("case \"pdf.rotateCounterClockwise\":"));
            Assert.That(script, Does.Contain("case \"pdf.toggleHandTool\":"));
            Assert.That(script, Does.Contain("bindToolbar()"));
            Assert.That(script, Does.Contain("renderToolbarState()"));
            Assert.That(script, Does.Not.Contain("statusCard"));
            Assert.That(wpf, Does.Contain("public Task SetThemeAsync(string theme) => _runtime.SetThemeAsync(theme);"));
            Assert.That(wpf, Does.Contain("OverlayableWebComponentControlBase"));
            Assert.That(wpf, Does.Contain("InitializeOverlaySurface(hostElement"));
            Assert.That(wpf, Does.Not.Contain("CreateButton(\"Prev\")"));
            Assert.That(wpf, Does.Not.Contain("Fit width"));
            Assert.That(wpf, Does.Not.Contain("WrapPanel"));
            Assert.That(avalonia, Does.Contain("public Task SetThemeAsync(string theme) => _runtime.SetThemeAsync(theme);"));
            Assert.That(winui, Does.Contain("public Task SetThemeAsync(string theme) => _runtime.SetThemeAsync(theme);"));
            Assert.That(showcase, Does.Contain("private string _theme;"));
            Assert.That(showcase, Does.Contain("public Task ApplyEnvironmentAsync(string theme)"));
            Assert.That(showcase, Does.Contain("InitialTheme = _theme"));
            Assert.That(showcase, Does.Contain("await _viewer.SetThemeAsync(_theme).ConfigureAwait(true);"));
            Assert.That(showcase, Does.Contain("UpdateViewerSurfaceHeight();"));
            Assert.That(showcase, Does.Contain("_viewerSurface.Height = double.NaN;"));
            Assert.That(showcase, Does.Contain("_viewerSurface.Height = Math.Max(360d, availableHeight);"));
            Assert.That(showcase, Does.Not.Contain("availableHeight * 0.25d"));
            Assert.That(showcase, Does.Contain("_viewerPanel.Width = _isFocusMode ? double.NaN : 504d;"));
            Assert.That(showcase, Does.Not.Contain("ApplyExpandButtonChrome"));
            Assert.That(showcase, Does.Not.Contain("SetResourceReference"));
            Assert.That(showcase, Does.Not.Contain("Color.FromArgb(188, 15, 23, 42)"));
            Assert.That(showcase, Does.Not.Contain("new SolidColorBrush(Color.FromArgb"));
            Assert.That(showcase, Does.Contain("new Binding(\"Foreground\")"));
            Assert.That(showcase, Does.Contain("CreateOverlayIcon"));
            Assert.That(showcase, Does.Not.Contain("Content = \"Expand demo\""));
            Assert.That(mainWindow, Does.Contain("await _pdfViewerShowcaseView.ApplyEnvironmentAsync(ResolveReportDesignerTheme()).ConfigureAwait(true);"));
            Assert.That(mainWindow, Does.Contain("_pdfViewerShowcaseView = new PdfViewerShowcaseView(ResolveReportDesignerTheme());"));
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
            }

            return directory.FullName;
        }
    }
}

