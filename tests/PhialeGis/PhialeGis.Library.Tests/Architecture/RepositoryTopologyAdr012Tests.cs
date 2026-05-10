using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace PhialeGis.Library.Tests.Architecture;

[TestFixture]
public sealed class RepositoryTopologyAdr012Tests
{
    private static readonly string[] RequiredTopLevelRoots =
    {
        "Docs",
        "src",
        "demo",
        "tests",
        "legacy",
        "scripts",
        "tools"
    };

    private static readonly string[] ForbiddenTopLevelActiveRoots =
    {
        "Wpf",
        "WinUI3",
        "Avalonia",
        "Features",
        "PhialeGis.Library.Core",
        "PhialeGis.Library.Actions",
        "PhialeGis.Library.Abstractions",
        "PhialeGis.Library.Domain",
        "PhialeGis.Library.Dsl",
        "PhialeGis.Library.Geometry",
        "PhialeGis.Library.Renderer.Skia",
        "PhialeGis.Library.Sync",
        "PhialeGis.WebAssets",
        "PhialeGis.Library.Tests",
        "PhialeGrid.Benchmarks",
        "PhialeGrid.MockServer",
        "PhialeGrid.MockServer.Tests"
    };

    private static readonly string[] ExpectedMovedDirectories =
    {
        @"src\PhialeGis\Core\PhialeGis.Library.Abstractions",
        @"src\PhialeGis\Core\PhialeGis.Library.Actions",
        @"src\PhialeGis\Core\PhialeGis.Library.Core",
        @"src\PhialeGis\Core\PhialeGis.Library.Domain",
        @"src\PhialeGis\Core\PhialeGis.Library.Dsl",
        @"src\PhialeGis\Core\PhialeGis.Library.Geometry",
        @"src\PhialeGis\Core\PhialeGis.Library.Renderer.Skia",
        @"src\PhialeGis\Core\PhialeGis.Library.Sync",
        @"src\PhialeGis\Core\PhialeGis.WebAssets",
        @"src\PhialeGis\Features\PhialeGis.Library.DslEditor",
        @"src\PhialeGis\Platforms\Wpf\PhialeGis.Library.WpfUi",
        @"src\PhialeGis\Platforms\WinUI3\PhialeGis.Library.WinUi",
        @"src\PhialeGis\Platforms\Avalonia\PhialeGis.Library.AvaloniaUi",
        @"tests\PhialeGis\PhialeGis.Library.Tests"
    };

    [Test]
    public void TopLevelRoots_MustContainRequiredRoots()
    {
        string repositoryRoot = ResolveRepositoryRoot();
        string[] topLevelDirectories = Directory.GetDirectories(repositoryRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        foreach (string requiredRoot in RequiredTopLevelRoots)
        {
            Assert.That(topLevelDirectories, Contains.Item(requiredRoot),
                $"Missing top-level root directory '{requiredRoot}'.");
        }
    }

    [Test]
    public void TopLevelRoots_MustNotContainLegacyArchitecturalRoots()
    {
        string repositoryRoot = ResolveRepositoryRoot();
        string[] topLevelDirectories = Directory.GetDirectories(repositoryRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        foreach (string forbiddenRoot in ForbiddenTopLevelActiveRoots)
        {
            Assert.That(topLevelDirectories, Does.Not.Contain(forbiddenRoot),
                $"Forbidden top-level root '{forbiddenRoot}' still exists.");
        }
    }

    [Test]
    public void MappedDirectories_MustExistInTargetLocations()
    {
        string repositoryRoot = ResolveRepositoryRoot();

        foreach (string relativePath in ExpectedMovedDirectories)
        {
            string absolutePath = Path.Combine(repositoryRoot, relativePath);
            Assert.That(Directory.Exists(absolutePath), Is.True,
                $"Expected mapped directory missing: '{relativePath}'.");
        }
    }

    [Test]
    public void ActiveProjects_MustStayUnderAllowedTopLevelRoots()
    {
        string repositoryRoot = ResolveRepositoryRoot();
        var allowedRoots = new HashSet<string>(RequiredTopLevelRoots, StringComparer.OrdinalIgnoreCase);

        foreach (string projectPath in Directory.GetFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(repositoryRoot, projectPath);
            string[] segments = relativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            string topLevel = segments[0];
            Assert.That(allowedRoots.Contains(topLevel), Is.True,
                $"Project '{relativePath}' is under forbidden top-level root '{topLevel}'.");
        }
    }

    [Test]
    public void MainSolution_ProjectPaths_MustUseAllowedTopLevelRootsOnly()
    {
        string repositoryRoot = ResolveRepositoryRoot();
        string solutionPath = Path.Combine(repositoryRoot, "PhialeGis.Library.sln");
        Assert.That(File.Exists(solutionPath), Is.True, "Main solution file is missing.");

        var allowedRoots = new HashSet<string>(RequiredTopLevelRoots, StringComparer.OrdinalIgnoreCase);
        string[] lines = File.ReadAllLines(solutionPath);

        foreach (string line in lines.Where(static l => l.StartsWith("Project(", StringComparison.Ordinal)))
        {
            string[] parts = line.Split(',');
            if (parts.Length < 2)
            {
                continue;
            }

            string rawPath = parts[1].Trim();
            if (!rawPath.StartsWith("\"", StringComparison.Ordinal) ||
                !rawPath.EndsWith("\"", StringComparison.Ordinal))
            {
                continue;
            }

            string relativeProjectPath = rawPath.Trim('"');
            if (!relativeProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) &&
                !relativeProjectPath.EndsWith(".shproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] segments = relativeProjectPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(segments.Length, Is.GreaterThan(0), $"Invalid project path in solution: '{relativeProjectPath}'.");

            string topLevel = segments[0];
            Assert.That(allowedRoots.Contains(topLevel), Is.True,
                $"Solution project path '{relativeProjectPath}' uses forbidden top-level root '{topLevel}'.");
        }
    }

    private static string ResolveRepositoryRoot()
    {
        string? current = TestContext.CurrentContext.TestDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "PhialeGis.Library.sln")))
            {
                return current;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        throw new InvalidOperationException("Could not resolve repository root from test directory.");
    }
}

