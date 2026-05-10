using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class ComparisonLabCompositionInputArchitectureTests
    {
        [Test]
        public void ComparisonLab_CompositionHost_FollowsOfficialSamplePattern_WithoutManualFocusForwarding()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("new WebView2CompositionControl"));
            Assert.That(code, Does.Not.Contain("PreviewMouseLeftButtonDown += HandlePreviewMouseLeftButtonDown"));
            Assert.That(code, Does.Not.Contain("Keyboard.Focus(_webView);"));
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
                "demo",
                "PhialeTech",
                "Wpf",
                "PhialeTech.WebHost.Wpf.ComparisonLab",
                "ComparisonHosts.cs");
        }
    }
}

