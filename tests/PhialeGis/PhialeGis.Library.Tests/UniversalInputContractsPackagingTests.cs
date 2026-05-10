using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests
{
    public class UniversalInputContractsPackagingTests
    {
        [Test]
        public void UniversalInputContracts_Assembly_DoesNotReferencePhialeGisAssemblies()
        {
            var references = typeof(UniversalPoint).Assembly
                .GetReferencedAssemblies()
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            Assert.That(
                references.Any(x => x.StartsWith("PhialeGis.", StringComparison.Ordinal)),
                Is.False,
                "UniversalInput.Contracts should stay extractable without depending on PhialeGis assemblies.");
        }

        [Test]
        public void UniversalInputContracts_Project_IsStandaloneAndPackable()
        {
            var repoRoot = RepositoryPaths.GetRepositoryRoot();
            var projectPath = Path.Combine(repoRoot, "src", "PhialeTech", "Shared", "UniversalInput.Contracts", "UniversalInput.Contracts.csproj");
            var readmePath = Path.Combine(repoRoot, "src", "PhialeTech", "Shared", "UniversalInput.Contracts", "README.md");
            var document = XDocument.Load(projectPath);

            var propertyValues = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "PropertyGroup")
                .Elements()
                .ToDictionary(x => x.Name.LocalName, x => (x.Value ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

            Assert.That(File.Exists(readmePath), Is.True);
            Assert.That(propertyValues["TargetFramework"], Is.EqualTo("netstandard2.0"));
            Assert.That(propertyValues["PackageId"], Is.EqualTo("UniversalInput.Contracts"));
            Assert.That(propertyValues["IsPackable"], Is.EqualTo("true"));
            Assert.That(propertyValues["GeneratePackageOnBuild"], Is.EqualTo("true"));
            Assert.That(propertyValues["PackageReadmeFile"], Is.EqualTo("README.md"));
            Assert.That(propertyValues["PackageLicenseExpression"], Is.EqualTo("MIT"));

            var projectReferenceCount = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "ItemGroup")
                .Elements()
                .Count(x => x.Name.LocalName == "ProjectReference");

            var packageReferenceCount = document.Root
                .Elements()
                .Where(x => x.Name.LocalName == "ItemGroup")
                .Elements()
                .Count(x => x.Name.LocalName == "PackageReference");

            Assert.That(projectReferenceCount, Is.EqualTo(0));
            Assert.That(packageReferenceCount, Is.EqualTo(0));
        }
    }
}

