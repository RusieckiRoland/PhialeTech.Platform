using NUnit.Framework;
using PhialeTech.ComponentHost.Definitions;
using PhialeTech.ComponentHost.State;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeGis.Library.Tests.Definitions
{
    [TestFixture]
    public sealed class DefinitionManagerTests
    {
        [Test]
        public void DefinitionManager_ShouldResolveDefinitionByKey()
        {
            var manager = new DefinitionManager(new[]
            {
                new InMemoryDefinitionSource("demo-local")
                    .Add("demo.grid.grouping", new DemoComponentDefinition(
                        definitionKind: "screen",
                        componentId: "grid",
                        titleKey: "Example.Grouping.Title",
                        summaryKey: "Example.Grouping.Description",
                        consumerHintKey: "consumer",
                        stateOverlayHintKey: "state"))
            });

            var resolved = manager.Resolve<DemoComponentDefinition>("demo.grid.grouping");

            Assert.Multiple(() =>
            {
                Assert.That(resolved.DefinitionKey, Is.EqualTo("demo.grid.grouping"));
                Assert.That(resolved.SourceId, Is.EqualTo("demo-local"));
                Assert.That(resolved.Definition.ComponentId, Is.EqualTo("grid"));
                Assert.That(resolved.Definition.DefinitionKind, Is.EqualTo("screen"));
            });
        }

        [Test]
        public void DefinitionManager_ShouldHandleUnknownDefinitionKeyCleanly()
        {
            var manager = new DefinitionManager(new[] { new InMemoryDefinitionSource("demo-local") });

            var resolved = manager.TryResolve<DemoComponentDefinition>("demo.unknown", out var resolution);

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.False);
                Assert.That(resolution, Is.Null);
            });
        }

        [Test]
        public void DefinitionManager_ShouldAllowMultipleSourcesWithoutRedesign()
        {
            var firstSource = new InMemoryDefinitionSource("local-source")
                .Add("demo.definition-manager", new DemoComponentDefinition(
                    definitionKind: "page",
                    componentId: "definition-manager",
                    titleKey: "Example.DefinitionManager.Title",
                    summaryKey: "Example.DefinitionManager.Description",
                    consumerHintKey: "consumer",
                    stateOverlayHintKey: "state"));
            var secondSource = new InMemoryDefinitionSource("remote-source")
                .Add("demo.grid.grouping", new DemoComponentDefinition(
                    definitionKind: "screen",
                    componentId: "grid",
                    titleKey: "Example.Grouping.Title",
                    summaryKey: "Example.Grouping.Description",
                    consumerHintKey: "consumer",
                    stateOverlayHintKey: "state"));
            var manager = new DefinitionManager(new[] { firstSource, secondSource });

            var resolved = manager.Resolve<DemoComponentDefinition>("demo.grid.grouping");

            Assert.That(resolved.SourceId, Is.EqualTo("remote-source"));
        }

        [Test]
        public void DemoApplicationServices_ShouldKeepDefinitionManagerSeparateFromApplicationStateManager()
        {
            using var services = DemoApplicationServices.CreateIsolatedForWindow();

            Assert.Multiple(() =>
            {
                Assert.That(services.DefinitionManager, Is.Not.Null);
                Assert.That(services.ApplicationStateManager, Is.Not.Null);
                Assert.That(services.DefinitionManager, Is.Not.InstanceOf<ApplicationStateManager>());
            });
        }
    }
}

