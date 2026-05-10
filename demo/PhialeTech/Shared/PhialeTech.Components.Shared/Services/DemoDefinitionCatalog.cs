using PhialeTech.ComponentHost.Definitions;
using PhialeTech.Components.Shared.Localization;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoDefinitionCatalog
    {
        public static DefinitionManager CreateManager()
        {
            return new DefinitionManager(new[] { CreateSource() });
        }

        public static InMemoryDefinitionSource CreateSource()
        {
            return new InMemoryDefinitionSource("demo-local")
                .Add("demo.definition-manager", new DemoComponentDefinition(
                    definitionKind: "page",
                    componentId: "definition-manager",
                    titleKey: DemoTextKeys.ExampleDefinitionManagerTitle,
                    summaryKey: DemoTextKeys.ExampleDefinitionManagerDescription,
                    consumerHintKey: DemoTextKeys.DefinitionManagerDefinitionConsumerHint,
                    stateOverlayHintKey: DemoTextKeys.DefinitionManagerDefinitionStateBoundary,
                    responsibilityTextKeys: new[]
                    {
                        DemoTextKeys.DefinitionManagerPageResponsibilityResolve,
                        DemoTextKeys.DefinitionManagerPageResponsibilityExplainSource,
                        DemoTextKeys.DefinitionManagerPageResponsibilityBuildBaseUi,
                    },
                    outOfScopeTextKeys: new[]
                    {
                        DemoTextKeys.DefinitionManagerPageOutOfScopeState,
                        DemoTextKeys.DefinitionManagerPageOutOfScopeRules,
                        DemoTextKeys.DefinitionManagerPageOutOfScopeData,
                    },
                    fields: new[]
                    {
                        new DemoDefinitionField("component", "definition-manager"),
                        new DemoDefinitionField("render.intent", "knowledge-page"),
                        new DemoDefinitionField("future.source", "yaml|file|remote"),
                    }))
                .Add("demo.grid.grouping", new DemoComponentDefinition(
                    definitionKind: "screen",
                    componentId: "grid",
                    titleKey: DemoTextKeys.ExampleGroupingTitle,
                    summaryKey: DemoTextKeys.ExampleGroupingDescription,
                    consumerHintKey: DemoTextKeys.DefinitionManagerGroupingConsumerHint,
                    stateOverlayHintKey: DemoTextKeys.DefinitionManagerGroupingStateBoundary,
                    responsibilityTextKeys: new[]
                    {
                        DemoTextKeys.DefinitionManagerGroupingResponsibilityGrouping,
                        DemoTextKeys.DefinitionManagerGroupingResponsibilityColumns,
                    },
                    outOfScopeTextKeys: new[]
                    {
                        DemoTextKeys.DefinitionManagerGroupingOutOfScopeState,
                    },
                    fields: new[]
                    {
                        new DemoDefinitionField("component", "grid"),
                        new DemoDefinitionField("screen.id", "grouping"),
                        new DemoDefinitionField("default.group", "Category"),
                        new DemoDefinitionField("toolbar.mode", "grouping-surface"),
                    }))
                .Add("demo.application-state-manager", new DemoComponentDefinition(
                    definitionKind: "page",
                    componentId: "application-state-manager",
                    titleKey: DemoTextKeys.ExampleApplicationStateManagerTitle,
                    summaryKey: DemoTextKeys.ExampleApplicationStateManagerDescription,
                    consumerHintKey: DemoTextKeys.DefinitionManagerApplicationStateConsumerHint,
                    stateOverlayHintKey: DemoTextKeys.DefinitionManagerApplicationStateBoundary,
                    responsibilityTextKeys: new[]
                    {
                        DemoTextKeys.DefinitionManagerApplicationStateResponsibilityUiState,
                    },
                    outOfScopeTextKeys: new[]
                    {
                        DemoTextKeys.DefinitionManagerApplicationStateOutOfScopeDefinitions,
                    },
                    fields: new[]
                    {
                        new DemoDefinitionField("component", "application-state-manager"),
                        new DemoDefinitionField("state.kind", "user-ui-overlay"),
                    }));
        }
    }
}

