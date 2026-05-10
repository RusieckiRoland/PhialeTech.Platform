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

        [Test]
        public void MainWindow_ReadsLiveYamlFromActiveMonacoEditor_BeforeRendering()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain("ReadLiveYamlGeneratedFormSourceTextAsync"));
            Assert.That(code, Does.Contain("await editor.GetValueAsync().ConfigureAwait(true)"));
            Assert.That(code, Does.Contain("var yamlSource = await ReadLiveYamlGeneratedFormSourceTextAsync().ConfigureAwait(true);"));
            Assert.That(code, Does.Contain("Payload = yamlSource"));
        }

        [Test]
        public void DemoYamlMonacoEditors_UseDedicatedWebComponentScrollHostAsTheirOnlyChrome()
        {
            var xaml = File.ReadAllText(GetLinkedMainWindowXamlPath());

            Assert.That(xaml, Does.Match("(?s)<webHost:PhialeWebComponentScrollHost Height=\"520\"\\s+Margin=\"0,16,0,0\"[^>]*>\\s*<webHost:PhialeWebComponentScrollHost.HostedContent>\\s*<ContentControl x:Name=\"YamlActionsEditorPresenter\""));
            Assert.That(xaml, Does.Match("(?s)<webHost:PhialeWebComponentScrollHost Height=\"520\"\\s+Margin=\"0,16,0,0\"[^>]*>\\s*<webHost:PhialeWebComponentScrollHost.HostedContent>\\s*<ContentControl x:Name=\"YamlGeneratedFormEditorPresenter\""));
            Assert.That(xaml, Does.Not.Contain("<Border MinHeight=\"520\"\r\n                                                                    Margin=\"0,16,0,0\""));
            Assert.That(xaml, Does.Not.Contain("ClipToBounds=\"True\">\r\n                                                                <webHost:PhialeWebComponentScrollHost Height=\"520\""));
        }

        [Test]
        public void DemoYamlMonacoEditors_UseStyleDrivenViewportInset()
        {
            var xaml = File.ReadAllText(GetLinkedMainWindowXamlPath());
            var styles = File.ReadAllText(GetDemoControlsStylePath());

            Assert.That(xaml, Does.Contain("Style=\"{StaticResource DemoYamlMonacoScrollHostStyle}\""));
            Assert.That(styles, Does.Contain("x:Key=\"DemoYamlMonacoScrollHostStyle\""));
            Assert.That(styles, Does.Contain("Property=\"ViewportPadding\" Value=\"{DynamicResource Thickness.WebComponentScrollHost.ViewportPadding}\""));
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

        private static string GetLinkedMainWindowXamlPath()
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

        private static string GetDemoControlsStylePath()
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
                "Themes",
                "Demo.Controls.xaml");
        }
    }
}

