using System;
using System.IO;
using NUnit.Framework;

namespace PhialeTech.Shell.Tests.Presentation
{
    [TestFixture]
    public sealed class ShellLayoutArchitectureTests
    {
        [Test]
        public void ShellGenericDictionary_ShouldStretchPhialeWindowContent()
        {
            var generic = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes.Linked/Shell/Generic.xaml");

            Assert.That(generic, Does.Contain("<Style TargetType=\"{x:Type controls:PhialeWindow}\">"));
            Assert.That(generic, Does.Contain("<Setter Property=\"HorizontalContentAlignment\" Value=\"Stretch\" />"));
            Assert.That(generic, Does.Contain("<Setter Property=\"VerticalContentAlignment\" Value=\"Stretch\" />"));
            Assert.That(generic, Does.Contain("<Setter Property=\"UseLayoutRounding\" Value=\"True\" />"));
            Assert.That(generic, Does.Contain("<Setter Property=\"SnapsToDevicePixels\" Value=\"True\" />"));
        }

        [Test]
        public void ShellGenericDictionary_ShouldStretchPhialeAppShellContent()
        {
            var generic = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes.Linked/Shell/Generic.xaml");
            var appShellStyleIndex = generic.IndexOf("<Style TargetType=\"{x:Type controls:PhialeAppShell}\">", StringComparison.Ordinal);

            Assert.That(appShellStyleIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(generic.IndexOf("<Setter Property=\"HorizontalContentAlignment\" Value=\"Stretch\" />", appShellStyleIndex, StringComparison.Ordinal), Is.GreaterThan(appShellStyleIndex));
            Assert.That(generic.IndexOf("<Setter Property=\"VerticalContentAlignment\" Value=\"Stretch\" />", appShellStyleIndex, StringComparison.Ordinal), Is.GreaterThan(appShellStyleIndex));
        }

        [Test]
        public void DemoWindow_ShouldKeepModalHostAtRootGridScope()
        {
            var window = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes.Linked/Demo/PhialeTech.Components.Wpf.MainWindow.xaml");
            var shellEndIndex = window.IndexOf("</shell:PhialeAppShell>", StringComparison.Ordinal);
            var modalHostIndex = window.IndexOf("<componentHost:PhialeModalLayerHost Grid.ColumnSpan=\"2\"", StringComparison.Ordinal);

            Assert.That(shellEndIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(modalHostIndex, Is.GreaterThan(shellEndIndex));
            Assert.That(window, Does.Contain("Service=\"{Binding RelativeSource={RelativeSource AncestorType=Window}, Path=HostedSurfaceService}\""));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var current = TestContext.CurrentContext.TestDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "PhialeGis.Library.sln")))
                {
                    return File.ReadAllText(Path.Combine(current, relativePath));
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            throw new InvalidOperationException("Could not resolve repository root.");
        }
    }
}

