using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.State;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Grid
{
    [TestFixture]
    public sealed class GridRegionSubsystemArchitectureTests
    {
        [Test]
        public void CoreRegionContracts_DoNotLeakWpfOrPlatformSpecificTypes()
        {
            var contractTypes = new[]
            {
                typeof(GridRegionDefinition),
                typeof(GridRegionLayoutManager),
                typeof(GridRegionViewState),
                typeof(GridRegionLayoutState),
                typeof(GridRegionLayoutSnapshot),
                typeof(GridRegionCommandInput),
            };

            foreach (var type in contractTypes)
            {
                Assert.That(type.Namespace, Does.StartWith("PhialeGrid.Core."));

                var memberTypes = type.GetProperties()
                    .Select(property => property.PropertyType)
                    .Concat(type.GetConstructors()
                        .SelectMany(ctor => ctor.GetParameters())
                        .Select(parameter => parameter.ParameterType))
                    .Distinct()
                    .ToArray();

                foreach (var memberType in memberTypes)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(memberType.Namespace ?? string.Empty, Does.Not.StartWith("System.Windows"), $"{type.Name} leaks {memberType}.");
                        Assert.That(memberType.FullName ?? string.Empty, Does.Not.Contain("Wpf"), $"{type.Name} leaks {memberType}.");
                    });
                }
            }
        }

        [Test]
        public void WpfRegionInfrastructure_RemainsInternalAndIsolatedFromPublicApi()
        {
            var repoRoot = RepositoryPaths.GetRepositoryRoot();
            var regionFiles = Directory.EnumerateFiles(
                    Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "Regions"),
                    "*.cs",
                    SearchOption.TopDirectoryOnly)
                .ToArray();

            Assert.That(regionFiles, Is.Not.Empty);

            foreach (var file in regionFiles)
            {
                var contents = File.ReadAllText(file);
                Assert.Multiple(() =>
                {
                    Assert.That(contents, Does.Contain("namespace PhialeTech.PhialeGrid.Wpf.Regions"), $"Unexpected namespace in {file}.");
                    Assert.That(contents, Does.Not.Contain("public sealed class WpfGrid"), $"WPF region helper should not be public: {file}.");
                    Assert.That(contents, Does.Not.Contain("public readonly struct WpfGrid"), $"WPF region helper should not be public: {file}.");
                    Assert.That(contents, Does.Not.Contain("public static class WpfGrid"), $"WPF region helper should not be public: {file}.");
                });
            }
        }

        [Test]
        public void TransitionalRegionTypes_AreRemovedAndNotReferenced()
        {
            var repoRoot = RepositoryPaths.GetRepositoryRoot();
            var removedFiles = new[]
            {
                Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "Regions", "GridRegionLayoutController.cs"),
                Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "Regions", "GridRegionOptions.cs"),
                Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "Regions", "GridRegionResolvedPolicy.cs"),
                Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "Regions", "GridRegionPresentationState.cs"),
            };

            foreach (var file in removedFiles)
            {
                Assert.That(File.Exists(file), Is.False, $"Removed transitional file still exists: {file}");
            }

            var codeFiles = Directory.EnumerateFiles(Path.Combine(repoRoot, "src"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.Combine(repoRoot, "tests"), "*.cs", SearchOption.AllDirectories))
                .Where(path => !string.Equals(Path.GetFileName(path), nameof(GridRegionSubsystemArchitectureTests) + ".cs", StringComparison.Ordinal))
                .ToArray();

            foreach (var file in codeFiles)
            {
                var contents = File.ReadAllText(file);
                Assert.Multiple(() =>
                {
                    Assert.That(contents, Does.Not.Contain("GridRegionLayoutController"), $"Transitional type reference found in {file}.");
                    Assert.That(contents, Does.Not.Contain("GridRegionResolvedPolicy"), $"Transitional type reference found in {file}.");
                    Assert.That(contents, Does.Not.Contain("GridRegionPresentationState"), $"Transitional type reference found in {file}.");
                });
            }
        }

        [Test]
        public void CoreRegionSourceFiles_DoNotDependOnWpfNamespaces()
        {
            var repoRoot = RepositoryPaths.GetRepositoryRoot();
            var files = Directory.EnumerateFiles(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core"), "*.cs", SearchOption.AllDirectories)
                .Where(path => path.Contains(Path.Combine("Regions")) || path.Contains("GridRegion") || path.EndsWith("GridSurfaceCoordinator.cs", StringComparison.Ordinal))
                .ToArray();

            foreach (var file in files)
            {
                var contents = File.ReadAllText(file);
                Assert.Multiple(() =>
                {
                    Assert.That(contents, Does.Not.Contain("System.Windows"), $"Core file leaks WPF namespace: {file}");
                    Assert.That(contents, Does.Not.Contain("PhialeTech.PhialeGrid.Wpf"), $"Core file leaks WPF namespace: {file}");
                    Assert.That(contents, Does.Not.Match(@"\bVisibility\b"), $"Core file leaks WPF primitive: {file}");
                    Assert.That(contents, Does.Not.Match(@"\bTranslateTransform\b"), $"Core file leaks WPF primitive: {file}");
                    Assert.That(contents, Does.Not.Contain("System.Windows.Controls.RowDefinition"), $"Core file leaks WPF primitive: {file}");
                    Assert.That(contents, Does.Not.Contain("System.Windows.Controls.ColumnDefinition"), $"Core file leaks WPF primitive: {file}");
                });
            }
        }
    }
}
