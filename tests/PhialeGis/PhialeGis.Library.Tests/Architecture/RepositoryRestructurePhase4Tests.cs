using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.Architecture
{
    [TestFixture]
    public sealed class RepositoryRestructurePhase4Tests
    {
        [Test]
        public void DemoProjects_ShouldLiveUnderDemoBucket()
        {
            var root = GetRepoRoot();

            Assert.That(File.Exists(Path.Combine(root, "demo", "PhialeTech", "Shared", "PhialeTech.Components.Shared", "PhialeTech.Components.Shared.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "demo", "PhialeTech", "Wpf", "PhialeTech.Components.Wpf", "PhialeTech.Components.Wpf.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "demo", "PhialeTech", "WinUI3", "PhialeTech.Components.WinUI", "PhialeTech.Components.WinUI.csproj")), Is.True);
            Assert.That(File.Exists(Path.Combine(root, "demo", "PhialeTech", "Avalonia", "PhialeTech.Components.Avalonia", "PhialeTech.Components.Avalonia.csproj")), Is.True);

            Assert.That(Directory.Exists(Path.Combine(root, "PhialeTech.Components.Shared")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Wpf", "PhialeTech.Components.Wpf")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "WinUI3", "PhialeTech.Components.WinUI")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "Avalonia", "PhialeTech.Components.Avalonia")), Is.False);
        }

        [Test]
        public void Solutions_ShouldReferenceDemoBucketProjects()
        {
            var root = GetRepoRoot();
            var demoSln = File.ReadAllText(Path.Combine(root, "PhialeTech.Components.sln"));

            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\Shared\PhialeTech.Components.Shared\PhialeTech.Components.Shared.csproj"));
            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\Wpf\PhialeTech.Components.Wpf\PhialeTech.Components.Wpf.csproj"));
            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\WinUI3\PhialeTech.Components.WinUI\PhialeTech.Components.WinUI.csproj"));
            Assert.That(demoSln, Does.Contain(@"demo\PhialeTech\Avalonia\PhialeTech.Components.Avalonia\PhialeTech.Components.Avalonia.csproj"));
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}
