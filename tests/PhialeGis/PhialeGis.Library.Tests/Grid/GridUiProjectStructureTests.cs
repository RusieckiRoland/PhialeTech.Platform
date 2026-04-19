using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridUiProjectStructureTests
    {
        [Test]
        public void GridUiProjects_ArePlacedInPlatformFolders()
        {
            var repoRoot = GetRepoRoot();

            Assert.That(File.Exists(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Localization", "PhialeGrid.Localization", "PhialeGrid.Localization.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.Wpf.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "Avalonia", "PhialeGrid.Avalonia", "PhialeGrid.Avalonia.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "WinUI3", "PhialeGrid.WinUI", "PhialeGrid.WinUI.csproj")), Is.True);
        }

        [Test]
        public void GridWpfUiProject_IsMultiTargetedForNet48AndModernWpf()
        {
            var repoRoot = GetRepoRoot();
            var projectPath = Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.Wpf.csproj");
            var document = XDocument.Load(projectPath);

            var targetFrameworks = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "PropertyGroup")
                .Elements()
                .First(x => x.Name.LocalName == "TargetFrameworks")
                .Value;

            Assert.That(targetFrameworks.Split(';'), Does.Contain("net48"));
            Assert.That(targetFrameworks.Split(';'), Does.Contain("net8.0-windows10.0.19041.0"));
        }

        [Test]
        public void GridProjects_ExposePhialeTechPackageIdentity()
        {
            var repoRoot = GetRepoRoot();

            AssertPackageId(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Core", "PhialeGrid.Core", "PhialeGrid.Core.csproj"), "PhialeTech.Grid.Core");
            AssertPackageId(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Localization", "PhialeGrid.Localization", "PhialeGrid.Localization.csproj"), "PhialeTech.Grid.Localization");
            AssertPackageId(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "Wpf", "PhialeGrid.Wpf", "PhialeGrid.Wpf.csproj"), "PhialeTech.Grid.Wpf");
            AssertPackageId(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "Avalonia", "PhialeGrid.Avalonia", "PhialeGrid.Avalonia.csproj"), "PhialeTech.Grid.Avalonia");
            AssertPackageId(Path.Combine(repoRoot, "src", "PhialeTech", "Products", "Grid", "Platforms", "WinUI3", "PhialeGrid.WinUI", "PhialeGrid.WinUI.csproj"), "PhialeTech.Grid.WinUI");
        }

        private static void AssertPackageId(string projectPath, string expectedPackageId)
        {
            var document = XDocument.Load(projectPath);
            var packageId = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "PropertyGroup")
                .Elements()
                .First(x => x.Name.LocalName == "PackageId")
                .Value;

            Assert.That(packageId, Is.EqualTo(expectedPackageId), projectPath);
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
