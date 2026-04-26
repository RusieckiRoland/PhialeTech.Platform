using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class WebHostWpfArchitectureTests
    {
        [Test]
        public void WpfWebHost_UsesCompositionControl_InsteadOfStandardWebView2()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("WebView2CompositionControl"));
            Assert.That(code, Does.Not.Contain("new WebView2()"));
        }

        [Test]
        public void WpfWebHost_FollowsOfficialCompositionSample_WithoutManualFocusOverrides()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("Focusable = false"));
            Assert.That(code, Does.Not.Contain("IsTabStop = true"));
            Assert.That(code, Does.Not.Contain("PreviewMouseLeftButtonDown += HandlePreviewMouseLeftButtonDown"));
            Assert.That(code, Does.Not.Contain("Keyboard.Focus(_webView);"));
            Assert.That(code, Does.Contain("UseLayoutRounding = true"));
            Assert.That(code, Does.Contain("SnapsToDevicePixels = true"));
            Assert.That(code, Does.Contain("ZoomFactor = 1d"));
            Assert.That(code, Does.Not.Contain("RenderOptions.SetBitmapScalingMode"));
        }

        private static string GetCodePath()
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

            return Path.Combine(
                directory.FullName,
                "src",
                "PhialeTech",
                "Shared",
                "Platforms",
                "Wpf",
                "PhialeTech.WebHost.Wpf",
                "Controls",
                "PhialeWebComponentHost.cs");
        }
    }
}
