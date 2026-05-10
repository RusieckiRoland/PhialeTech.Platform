using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class ComparisonLabArchitectureTests
    {
        [Test]
        public void ComparisonLab_InitializesMonacoEditorsExplicitly_AfterLoaded()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("await editor.InitializeAsync()"));
            Assert.That(code, Does.Contain("await editor.SetThemeAsync(\"light\")"));
            Assert.That(code, Does.Contain("await editor.SetLanguageAsync(\"yaml\")"));
            Assert.That(code, Does.Contain("await editor.SetValueAsync(SampleCode)"));
            Assert.That(code, Does.Contain("Monaco initialization failed: "));
        }

        [Test]
        public void ComparisonLab_UsesCustomScrollHost_ForCompositionScrolledScenarios()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("new PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Contain("CreateScrolledSurface(true, \"scrolled.composition\", 340)"));
            Assert.That(code, Does.Contain("CreateTabbedScrolledSurface(true, \"tabscroll.composition\", 360)"));
        }

        [Test]
        public void ComparisonLab_UsesCustomScrollHost_AsGlobalRootContainer()
        {
            var code = File.ReadAllText(GetXamlPath());

            Assert.That(code, Does.Contain("controls:PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Contain("<controls:PhialeWebComponentScrollHost.HostedContent>"));
            Assert.That(code, Does.Not.Contain("<ScrollViewer"));
        }

        private static string GetCodePath()
        {
            return Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.WebHost.Wpf.ComparisonLab", "MainWindow.xaml.cs");
        }

        private static string GetXamlPath()
        {
            return Path.Combine(GetRepositoryRoot(), "demo", "PhialeTech", "Wpf", "PhialeTech.WebHost.Wpf.ComparisonLab", "MainWindow.xaml");
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

