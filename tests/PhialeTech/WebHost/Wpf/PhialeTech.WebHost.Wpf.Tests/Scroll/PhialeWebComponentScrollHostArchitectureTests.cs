using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests.Scroll
{
    public sealed class PhialeWebComponentScrollHostArchitectureTests
    {
        [Test]
        public void ScrollHost_IsImplementedInWpfFrontend_WithoutScrollViewer()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("sealed class PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Contain("namespace PhialeTech.WebHost.Wpf.Controls"));
            Assert.That(code, Does.Not.Contain("ScrollViewer"));
        }

        [Test]
        public void ScrollHost_RemainsNonFocusable_AndClipsViewport()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("Focusable = false"));
            Assert.That(code, Does.Contain("IsTabStop = false"));
            Assert.That(code, Does.Contain("ClipToBounds = true"));
            Assert.That(code, Does.Contain("PreviewMouseWheel += OnPreviewMouseWheel"));
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
                "PhialeWebComponentScrollHost.cs");
        }
    }
}
