using System.IO;
using NUnit.Framework;
using PhialeGis.Library.Tests.Support;

namespace PhialeGis.Library.Tests.ActiveLayerSelector
{
    [TestFixture]
    public sealed class ActiveLayerSelectorArchitectureTests
    {
        [Test]
        public void WpfHost_ShouldUseClickHandlersAndUniversalInputInsteadOfDirectViewModelCommands()
        {
            var xaml = File.ReadAllText(GetXamlPath());
            var codeBehind = File.ReadAllText(GetCodeBehindPath());

            Assert.That(xaml, Does.Contain("Click=\"HandleExpandCollapseClick\""));
            Assert.That(xaml, Does.Contain("Click=\"HandleShowMoreClick\""));
            Assert.That(xaml, Does.Contain("Click=\"HandleSetActiveClick\""));
            Assert.That(xaml, Does.Contain("Click=\"HandleCapabilityClick\""));
            Assert.That(xaml, Does.Not.Contain("Command=\"{Binding"));

            Assert.That(codeBehind, Does.Contain("HandleCommand("));
            Assert.That(codeBehind, Does.Contain("ActiveLayerSelectorUniversalInputAdapter.CreateCommand"));
            Assert.That(codeBehind, Does.Not.Contain("ToggleExpandedCommand.Execute"));
            Assert.That(codeBehind, Does.Not.Contain("ShowMoreCommand"));
        }

        [Test]
        public void CoreState_ShouldDependOnSharedHostAbstractions()
        {
            var project = File.ReadAllText(GetCoreProjectPath());
            var stateInterface = File.ReadAllText(GetStateInterfacePath());
            var itemState = File.ReadAllText(GetItemStatePath());

            Assert.That(project, Does.Contain(@"Shared\PhialeTech.ComponentHost.Abstractions\PhialeTech.ComponentHost.Abstractions.csproj"));
            Assert.That(stateInterface, Does.Contain("IPhialeLayerCollectionState<ActiveLayerSelectorItemState>"));
            Assert.That(itemState, Does.Contain(": IPhialeLayerState"));
        }

        private static string GetXamlPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "ActiveLayerSelector", "Platforms", "Wpf", "PhialeTech.ActiveLayerSelector.Wpf", "Controls", "ActiveLayerSelector.xaml");
        }

        private static string GetCodeBehindPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "ActiveLayerSelector", "Platforms", "Wpf", "PhialeTech.ActiveLayerSelector.Wpf", "Controls", "ActiveLayerSelector.xaml.cs");
        }

        private static string GetCoreProjectPath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "ActiveLayerSelector", "Core", "PhialeTech.ActiveLayerSelector", "PhialeTech.ActiveLayerSelector.csproj");
        }

        private static string GetStateInterfacePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "ActiveLayerSelector", "Core", "PhialeTech.ActiveLayerSelector", "IActiveLayerSelectorState.cs");
        }

        private static string GetItemStatePath()
        {
            return Path.Combine(GetRepoRoot(), "src", "PhialeTech", "Products", "ActiveLayerSelector", "Core", "PhialeTech.ActiveLayerSelector", "ActiveLayerSelectorItemState.cs");
        }

        private static string GetRepoRoot()
        {
            return RepositoryPaths.GetRepositoryRoot();
        }
    }
}

