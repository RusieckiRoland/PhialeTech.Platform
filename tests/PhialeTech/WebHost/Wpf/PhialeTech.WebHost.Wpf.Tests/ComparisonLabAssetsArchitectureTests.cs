using System.IO;
using NUnit.Framework;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class ComparisonLabAssetsArchitectureTests
    {
        [Test]
        public void ComparisonLab_Project_CopiesWebAssetsIntoOutput()
        {
            var code = File.ReadAllText(GetCodePath());

            Assert.That(code, Does.Contain(@"src\PhialeTech\Shared\PhialeTech.WebAssets\Assets\**\*"));
            Assert.That(code, Does.Contain(@"src\PhialeGis\Core\PhialeGis.WebAssets\Assets\Monaco\**\*"));
            Assert.That(code, Does.Contain("<Link>Assets\\%(RecursiveDir)%(Filename)%(Extension)</Link>"));
            Assert.That(code, Does.Contain("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>"));
        }

        private static string GetCodePath()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
            }

            return Path.Combine(
                directory.FullName,
                "demo",
                "PhialeTech",
                "Wpf",
                "PhialeTech.WebHost.Wpf.ComparisonLab",
                "PhialeTech.WebHost.Wpf.ComparisonLab.csproj");
        }
    }
}
