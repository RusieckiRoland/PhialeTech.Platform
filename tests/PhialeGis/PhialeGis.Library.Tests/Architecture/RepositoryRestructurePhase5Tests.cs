using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class RepositoryRestructurePhase5Tests
    {
        [Test]
        public void MainSolution_ShouldNotUseLegacyDemoPaths()
        {
            var root = GetRepoRoot();
            var mainSln = File.ReadAllText(Path.Combine(root, "PhialeGis.Library.sln"));

            Assert.That(mainSln, Does.Not.Contain(@"demo\Wpf\PhialeTech.Components.Wpf\PhialeTech.Components.Wpf.csproj"));
            Assert.That(mainSln, Does.Not.Contain(@"demo\WinUI3\PhialeTech.Components.WinUI\PhialeTech.Components.WinUI.csproj"));
            Assert.That(mainSln, Does.Not.Contain(@"demo\Avalonia\PhialeTech.Components.Avalonia\PhialeTech.Components.Avalonia.csproj"));
        }

        [Test]
        public void MainSolution_ShouldKeepLegacyUwpUnderLegacyRoot()
        {
            var root = GetRepoRoot();
            var mainSln = File.ReadAllText(Path.Combine(root, "PhialeGis.Library.sln"));

            Assert.That(mainSln, Does.Contain("= \"legacy\", \"legacy\""));
            Assert.That(mainSln, Does.Contain("= \"Uwp\", \"Uwp\""));
            Assert.That(mainSln, Does.Not.Contain(@"Uwp\PhialeGis.Library.UwpUi\PhialeGis.Library.UwpUi.csproj"));
        }

        [Test]
        public void DemoSolution_ShouldReferenceDemoBucketProjects()
        {
            var root = GetRepoRoot();
            var demoSln = File.ReadAllText(Path.Combine(root, "PhialeTech.Components.sln"));

            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\Shared\PhialeTech.Components.Shared\PhialeTech.Components.Shared.csproj"));
            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\Wpf\PhialeTech.Components.Wpf\PhialeTech.Components.Wpf.csproj"));
            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\WinUI3\PhialeTech.Components.WinUI\PhialeTech.Components.WinUI.csproj"));
            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\Avalonia\PhialeTech.Components.Avalonia\PhialeTech.Components.Avalonia.csproj"));
        }

        [Test]
        public void DemoSharedProject_ShouldUseProductAndSharedSources()
        {
            var root = GetRepoRoot();
            var project = File.ReadAllText(Path.Combine(root, "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "PhialeTech.Components.Shared.csproj"));

            Assert.That(project, Does.Contain(@"src\PhialeTech\Products\ActiveLayerSelector\Core\PhialeTech.ActiveLayerSelector\PhialeTech.ActiveLayerSelector.csproj"));
            Assert.That(project, Does.Contain(@"src\PhialeTech\Products\Grid\Core\PhialeGrid.Core\PhialeGrid.Core.csproj"));
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
