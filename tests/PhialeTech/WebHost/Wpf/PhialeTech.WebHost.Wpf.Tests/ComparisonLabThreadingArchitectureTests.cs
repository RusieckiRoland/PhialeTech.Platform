using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class ComparisonLabThreadingArchitectureTests
    {
        [Test]
        public void ComparisonLab_CreatesSharedWebViewEnvironment_OnUiThread_AndSignalsLoaded()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("_bridge.WarmUp();"));
            Assert.That(code, Does.Contain("_bridge.NotifyLoaded();"));
            Assert.That(code, Does.Contain("RunOnUiAsync(() => GetSharedEnvironmentAsync())"));
            Assert.That(code, Does.Not.Contain("_host.Loaded += HandleHostLoaded"));
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
