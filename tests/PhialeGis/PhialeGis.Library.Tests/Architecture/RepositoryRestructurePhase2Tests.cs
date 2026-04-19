using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class RepositoryRestructurePhase2Tests
    {
        [Test]
        public void UniversalInputContracts_ShouldBeUnderSrcShared()
        {
            var root = GetRepoRoot();

            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Shared", "UniversalInput.Contracts", "UniversalInput.Contracts.csproj")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "UniversalInput.Contracts")), Is.False);
        }

        [Test]
        public void Solutions_ShouldPointToMovedUniversalInputContracts()
        {
            var root = GetRepoRoot();
            var mainSln = File.ReadAllText(Path.Combine(root, "PhialeGis.Library.sln"));
            var demoSln = File.ReadAllText(Path.Combine(root, "PhialeTech.Components.sln"));

            Assert.That(mainSln, Does.Contain(@"src\PhialeTech\Shared\UniversalInput.Contracts\UniversalInput.Contracts.csproj"));
            Assert.That(demoSln, Does.Contain(@"src\PhialeTech\Shared\UniversalInput.Contracts\UniversalInput.Contracts.csproj"));
            Assert.That(mainSln, Does.Not.Contain("= \"UniversalInput.Contracts\", \"UniversalInput.Contracts\\UniversalInput.Contracts.csproj\""));
            Assert.That(demoSln, Does.Not.Contain("= \"UniversalInput.Contracts\", \"UniversalInput.Contracts\\UniversalInput.Contracts.csproj\""));
        }

        [Test]
        public void ProjectReferences_ShouldNotUseLegacyUniversalInputPath()
        {
            var root = GetRepoRoot();

            AssertProjectDoesNotContainLegacyPath(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Actions", "PhialeGis.Library.Actions.csproj"));
            AssertProjectDoesNotContainLegacyPath(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "PhialeGrid.Core.csproj"));
            AssertProjectDoesNotContainLegacyPath(Path.Combine(root, "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.Wpf.csproj"));
            AssertProjectDoesNotContainLegacyPath(Path.Combine(root, "src", "PhialeTech", "Products", "ActiveLayerSelector", "Core", "PhialeTech.ActiveLayerSelector", "PhialeTech.ActiveLayerSelector.csproj"));
        }

        private static void AssertProjectDoesNotContainLegacyPath(string projectPath)
        {
            var content = File.ReadAllText(projectPath);
            Assert.That(content, Does.Not.Contain(@"..\UniversalInput.Contracts\UniversalInput.Contracts.csproj"));
            Assert.That(content, Does.Not.Contain(@"..\..\UniversalInput.Contracts\UniversalInput.Contracts.csproj"));
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
