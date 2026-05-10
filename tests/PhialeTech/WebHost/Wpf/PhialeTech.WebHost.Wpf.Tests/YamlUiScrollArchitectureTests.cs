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

            Assert.That(code, Does.Contain("x:Name=\"DemoSurfaceViewportGrid\""));
            Assert.That(code, Does.Contain("<webHost:PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Contain("Visibility=\"{Binding ShowYamlUiSurface"));
            Assert.That(code, Does.Contain("Height=\"{Binding ActualHeight, ElementName=DemoSurfaceViewportGrid}\""));
            Assert.That(code, Does.Not.Contain("<ScrollViewer Visibility=\"{Binding ShowYamlUiSurface"));
        }

        [Test]
        public void YamlDocumentHost_UsesCustomScrollHost_ForLayoutRegion()
        {
            var code = File.ReadAllText(GetYamlDocumentHostPath());

            Assert.That(code, Does.Contain("new PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Contain("HostedContent = BuildScrollableContent(plan)"));
            Assert.That(code, Does.Contain("ResolveLayoutRegionHeight()"));
            Assert.That(code, Does.Contain("LayoutHeightMode.Auto"));
            Assert.That(code, Does.Contain("VerticalAlignment = IsLayoutHeightAuto()"));
            Assert.That(code, Does.Contain("var root = new Grid"));
            Assert.That(code, Does.Contain("OverlayHost.SetIsScope(root, true);"));
            Assert.That(code, Does.Contain("root.Children.Add(chrome);"));
            Assert.That(code, Does.Not.Contain("new ScrollViewer"));
            Assert.That(code, Does.Not.Contain("OnContentScrollViewerPreviewMouseWheel"));
            Assert.That(code, Does.Not.Contain("FindParentScrollViewer"));
        }

        [Test]
        public void HostedYamlModal_ReturnsYamlDocumentHostWithoutOuterScrollContainer()
        {
            var code = File.ReadAllText(GetHostedYamlSurfaceFactoryPath());

            Assert.That(code, Does.Contain("return host;"));
            Assert.That(code, Does.Not.Contain("new PhialeWebComponentScrollHost"));
            Assert.That(code, Does.Not.Contain("return new ScrollViewer"));
            Assert.That(code, Does.Not.Contain("Content = host"));
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

        private static string GetYamlDocumentHostPath()
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
                "Products",
                "YamlApp",
                "Platforms",
                "Wpf",
                "PhialeTech.YamlApp.Wpf",
                "Document",
                "YamlDocumentHost.cs");
        }

        private static string GetHostedYamlSurfaceFactoryPath()
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
                "Hosting",
                "DemoYamlHostedSurfaceFactory.cs");
        }
    }
}

