using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class RepositoryRestructurePhase6Tests
    {
        [Test]
        public void MainSolution_ShouldExposePhialeTechFolders_ForProductsAndSharedContracts()
        {
            var root = GetRepoRoot();
            var mainSln = File.ReadAllText(Path.Combine(root, "PhialeGis.Library.sln"));

            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Shared\PhialeTech.ComponentHost.Abstractions\PhialeTech.ComponentHost.Abstractions.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Core\PhialeGrid.Core\PhialeGrid.Core.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\ActiveLayerSelector\Core\PhialeTech.ActiveLayerSelector\PhialeTech.ActiveLayerSelector.csproj"));
        }

        [Test]
        public void MainSolution_ShouldReferenceSharedHostAbstractions_AndPhialeTechNamedGridProjects()
        {
            var root = GetRepoRoot();
            var mainSln = File.ReadAllText(Path.Combine(root, "PhialeGis.Library.sln"));

            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Shared\PhialeTech.ComponentHost.Abstractions\PhialeTech.ComponentHost.Abstractions.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Core\PhialeGrid.Core\PhialeGrid.Core.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Platforms\Wpf\PhialeGrid.Wpf\PhialeGrid.Wpf.csproj"));
            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Products\Grid\Localization\PhialeGrid.Localization\PhialeGrid.Localization.csproj"));
            Assert.That(mainSln, Does.Not.Contain(@"src\Shared\PhialeTech.ComponentHost.Abstractions\PhialeTech.ComponentHost.Abstractions.csproj"));
        }

        [Test]
        public void SharedHostAbstractionsProject_ShouldExistUnderSrcShared()
        {
            var root = GetRepoRoot();

            Assert.That(
                File.Exists(Path.Combine(root, "src", "PhialeTech", "Shared", "PhialeTech.ComponentHost.Abstractions", "PhialeTech.ComponentHost.Abstractions.csproj")),
                Is.True);
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

