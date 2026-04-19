using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class YamlUiScrollArchitectureTests
    {
        [Test]
        public void YamlUiSurface_UsesCustomScrollHost_InsteadOfOuterScrollViewer()
        {
            var code = File.ReadAllText(GetXamlPath());

            Assert.That(code, Does.Contain("<webHost:PhialeWebComponentScrollHost Visibility=\"{Binding ShowYamlUiSurface"));
            Assert.That(code, Does.Not.Contain("<ScrollViewer Visibility=\"{Binding ShowYamlUiSurface"));
        }

        private static string GetXamlPath()
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
                "PhialeTech.Styles.Wpf",
                "Themes.Linked",
                "Demo",
                "PhialeTech.Components.Wpf.MainWindow.xaml");
        }
    }
}
