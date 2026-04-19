using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class YamlAppStyleDictionaryArchitectureTests
    {
        [Test]
        public void GenericDictionary_ShouldMergePublicYamlAppStyleDictionary()
        {
            var generic = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes.Linked/YamlApp/Generic.xaml");

            Assert.That(generic, Does.Contain("Themes/YamlApp.Styles.xaml"));
            Assert.That(generic, Does.Contain("Themes/YamlApp.ControlStyles.xaml"));
            Assert.That(generic, Does.Contain("<Style TargetType=\"{x:Type buttons:YamlButton}\" BasedOn=\"{StaticResource YamlButton.DefaultStyle}\" />"));
        }

        [Test]
        public void PublicYamlAppStyleDictionary_ShouldContainRendererVisibleHelperResources()
        {
            var styles = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes/YamlApp.Styles.xaml");

            Assert.That(styles, Does.Contain("x:Key=\"YamlDocument.HeaderTitleTextStyle\""));
            Assert.That(styles, Does.Contain("x:Key=\"YamlDocument.LayoutShellRegionStyle\""));
            Assert.That(styles, Does.Contain("x:Key=\"YamlDocument.RowItemHostStyle\""));
        }

        [Test]
        public void YamlAppControlStyleDictionary_ShouldContainPublicControlSpecificHelperResources()
        {
            var styles = ReadRepositoryFile("src/PhialeTech/Products/YamlApp/Platforms/Wpf/PhialeTech.YamlApp.Wpf/Themes/YamlApp.ControlStyles.xaml");

            Assert.That(styles, Does.Contain("x:Key=\"YamlDocument.StatusBadgeStyle\""));
            Assert.That(styles, Does.Contain("x:Key=\"YamlDocument.ActionButtonStyle.Horizontal.Last\""));
            Assert.That(styles, Does.Contain("x:Key=\"YamlButton.DefaultStyle\""));
        }

        [Test]
        public void GenericDictionary_ShouldNotPublishKeyedHelperResourcesUsedByRenderer()
        {
            var generic = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes.Linked/YamlApp/Generic.xaml");

            Assert.That(generic, Does.Not.Contain("x:Key=\"YamlDocument.HeaderTitleTextStyle\""));
            Assert.That(generic, Does.Not.Contain("x:Key=\"YamlDocument.LayoutShellRegionStyle\""));
            Assert.That(generic, Does.Not.Contain("x:Key=\"YamlDocument.ActionButtonStyle.Horizontal.Last\""));
            Assert.That(generic, Does.Not.Contain("x:Key=\"YamlDocument.StatusBadgeStyle\""));
        }

        [Test]
        public void DemoApplication_ShouldLoadPublicYamlAppStyleDictionary()
        {
            var app = ReadRepositoryFile("demo/PhialeTech/Wpf/PhialeTech.Components.Wpf/App.xaml");

            Assert.That(app, Does.Contain("Themes/YamlApp.Styles.xaml"));
            Assert.That(app, Does.Contain("Themes/YamlApp.ControlStyles.xaml"));
        }

        [Test]
        public void DemoWindowThemeEntrypoint_ShouldLoadPublicYamlAppStyleDictionaries()
        {
            var window = ReadRepositoryFile("src/PhialeTech/Shared/PhialeTech.Styles.Wpf/Themes.Linked/Demo/PhialeTech.Components.Wpf.MainWindow.xaml");

            Assert.That(window, Does.Contain("Themes/YamlApp.Styles.xaml"));
            Assert.That(window, Does.Contain("Themes/YamlApp.ControlStyles.xaml"));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var root = RepositoryPaths.GetRepositoryRoot();
            return File.ReadAllText(Path.Combine(root, relativePath));
        }
    }
}
