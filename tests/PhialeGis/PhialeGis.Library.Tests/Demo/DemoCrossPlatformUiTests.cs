using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Demo
{
    public sealed class DemoCrossPlatformUiTests
    {
        [Test]
        public void WinUiDemoApp_ShouldCreateSharedDemoApplicationServices_AndPassSharedDefinitionManagerToMainWindow()
        {
            var appCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "WinUI3", "PhialeTech.Components.WinUI", "App.xaml.cs"));
            var mainWindowCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "WinUI3", "PhialeTech.Components.WinUI", "MainWindow.cs"));

            Assert.Multiple(() =>
            {
                Assert.That(appCode, Does.Contain("DemoApplicationServices.CreateDefault()"));
                Assert.That(appCode, Does.Contain("new MainWindow(_applicationServices)"));
                Assert.That(appCode, Does.Not.Contain("new DemoShellViewModel(\"WinUI\")"));
                Assert.That(mainWindowCode, Does.Contain("definitionManager: _applicationServices.DefinitionManager"));
            });
        }

        [Test]
        public void AvaloniaDemoApp_ShouldCreateSharedDemoApplicationServices_AndPassSharedDefinitionManagerToMainWindow()
        {
            var appCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Avalonia", "PhialeTech.Components.Avalonia", "App.axaml.cs"));
            var mainWindowCode = File.ReadAllText(Path.Combine(GetRepoRoot(), "demo", "PhialeTech", "Avalonia", "PhialeTech.Components.Avalonia", "MainWindow.axaml.cs"));

            Assert.Multiple(() =>
            {
                Assert.That(appCode, Does.Contain("DemoApplicationServices.CreateDefault()"));
                Assert.That(appCode, Does.Contain("new MainWindow(_applicationServices)"));
                Assert.That(mainWindowCode, Does.Not.Contain("new DemoShellViewModel(\"Avalonia\")"));
                Assert.That(mainWindowCode, Does.Contain("definitionManager: _applicationServices.DefinitionManager"));
            });
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

