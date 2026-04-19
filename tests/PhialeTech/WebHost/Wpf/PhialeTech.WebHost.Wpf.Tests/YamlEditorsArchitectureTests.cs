using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class YamlEditorsArchitectureTests
    {
        [Test]
        public void MainWindow_UsesSeparateMonacoEditors_ForYamlActionsAndYamlDocument()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("_yamlActionsMonacoEditor"));
            Assert.That(code, Does.Contain("_yamlDocumentMonacoEditor"));
            Assert.That(code, Does.Not.Contain("_yamlGeneratedFormMonacoEditor"));
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
                "PhialeTech.Components.Wpf",
                "MainWindow.xaml.cs");
        }
    }
}
