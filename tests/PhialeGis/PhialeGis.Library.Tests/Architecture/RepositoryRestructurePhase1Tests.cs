using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class RepositoryRestructurePhase1Tests
    {
        [Test]
        public void RepositoryBuckets_ShouldExist()
        {
            var root = GetRepoRoot();

            Assert.That(Directory.Exists(Path.Combine(root, "src")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "demo")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "tests")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "tools")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "legacy")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "Docs")), Is.True);
        }

        [Test]
        public void Uwp_ShouldBeUnderLegacy()
        {
            var root = GetRepoRoot();

            Assert.That(Directory.Exists(Path.Combine(root, "legacy", "Uwp")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "Uwp")), Is.False);
        }

        [Test]
        public void ActiveLayerSelector_ShouldUseProductPath()
        {
            var root = GetRepoRoot();

            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "ActiveLayerSelector", "Core", "PhialeTech.ActiveLayerSelector", "PhialeTech.ActiveLayerSelector.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "src", "PhialeTech", "Products", "ActiveLayerSelector", "Platforms", "Wpf", "PhialeTech.ActiveLayerSelector.Wpf", "PhialeTech.ActiveLayerSelector.Wpf.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "tests", "PhialeTech", "ActiveLayerSelector", "Wpf", "PhialeTech.ActiveLayerSelector.Wpf.Tests", "PhialeTech.ActiveLayerSelector.Wpf.Tests.csproj")), Is.True);

            Assert.That(Directory.Exists(Path.Combine(root, "PhialeTech.ActiveLayerSelector")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Wpf", "PhialeTech.ActiveLayerSelector.Wpf")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Wpf", "PhialeTech.ActiveLayerSelector.Wpf.Tests")), Is.False);
        }

        [Test]
        public void CoupledGisModules_ShouldRemainInCurrentPath()
        {
            var root = GetRepoRoot();

            Assert.That(Directory.Exists(Path.Combine(root, "src", "PhialeGis", "Core", "PhialeGis.Library.Dsl")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(root, "src", "PhialeGis", "Features", "PhialeGis.Library.DslEditor")), Is.True);
        }

        [Test]
        public void ProjectReferences_ShouldPointToMovedActiveLayerSelector()
        {
            var root = GetRepoRoot();
            var testsProject = File.ReadAllText(Path.Combine(root, "tests", "PhialeGis", "PhialeGis.Library.Tests", "PhialeGis.Library.Tests.csproj"));
            var wpfDemoProject = File.ReadAllText(Path.Combine(root, "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "PhialeTech.Components.Wpf.csproj"));

            Assert.That(testsProject, Does.Contain(@"src\PhialeTech\Products\ActiveLayerSelector\Core\PhialeTech.ActiveLayerSelector\PhialeTech.ActiveLayerSelector.csproj"));
            Assert.That(wpfDemoProject, Does.Contain(@"src\PhialeTech\Products\ActiveLayerSelector\Platforms\Wpf\PhialeTech.ActiveLayerSelector.Wpf\PhialeTech.ActiveLayerSelector.Wpf.csproj"));
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
