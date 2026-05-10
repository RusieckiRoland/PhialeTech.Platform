using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class RepositoryRestructurePhase3Tests
    {
        [Test]
        public void PhialeGridFamily_ShouldUseProductPath()
        {
            var root = GetRepoRoot();

            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "PhialeGrid.Core.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Localization", "PhialeGrid.Localization", "PhialeGrid.Localization.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.Wpf.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Platforms", "Avalonia", "PhialeGrid.Avalonia", "PhialeGrid.Avalonia.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Platforms", "WinUI3", "PhialeGrid.WinUI", "PhialeGrid.WinUI.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "tests", "PhialeTech", "Grid", "Wpf", "PhialeTech.Grid.Wpf.Tests", "PhialeGrid.Wpf.Tests.csproj")), Is.True);

            Assert.That(Directory.Exists(Path.Combine(root, "PhialeGrid.Core")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "PhialeGrid.Localization")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Wpf", "PhialeGrid.Wpf")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Avalonia", "PhialeGrid.Avalonia")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "WinUI3", "PhialeGrid.WinUI")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Wpf", "PhialeGrid.Wpf.Tests")), Is.False);
        }

        [Test]
        public void Solutions_ShouldPointToMovedPhialeGridProjects()
        {
            var root = GetRepoRoot();
            var mainSln = File.ReadAllText(Path.Combine(root, "PhialeGis.Library.sln"));
            var demoSln = File.ReadAllText(Path.Combine(root, "PhialeTech.Components.sln"));

            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Core\PhialeGrid.Core\PhialeGrid.Core.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Localization\PhialeGrid.Localization\PhialeGrid.Localization.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Platforms\Wpf\PhialeGrid.Wpf\PhialeGrid.Wpf.csproj"));
            Assert.That(mainSln, Does.Contain(@"tests\PhialeTech\Grid\Wpf\PhialeTech.Grid.Wpf.Tests\PhialeGrid.Wpf.Tests.csproj"));

            Assert.That(demoSln, Does.Contain(@"src\PhialeTech\Products\Grid\Core\PhialeGrid.Core\PhialeGrid.Core.csproj"));
            Assert.That(demoSln, Does.Contain(@"src\PhialeTech\Products\Grid\Platforms\Avalonia\PhialeGrid.Avalonia\PhialeGrid.Avalonia.csproj"));
            Assert.That(demoSln, Does.Contain(@"src\PhialeTech\Products\Grid\Platforms\WinUI3\PhialeGrid.WinUI\PhialeGrid.WinUI.csproj"));
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

