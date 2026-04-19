using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Rendering;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Definitions.Documents;

namespace PhialeGis.Library.Tests.YamlApp
{
    [TestFixture]
    public sealed class DocumentActionRenderPlanBuilderTests
    {
        [Test]
        public void Build_ShouldGroupVisibleActionsIntoDeclaredAreas_AndSortByOrder()
        {
            var builder = new DocumentActionRenderPlanBuilder();
            var document = CreateDocument(
                actionAreas: new[]
                {
                    CreateArea("header", ActionAlignment.Left),
                    CreateArea("footer", ActionAlignment.Right),
                },
                actions: new[]
                {
                    CreateAction("cancel", "footer", order: 20, semantic: ActionSemantic.Cancel),
                    CreateAction("save", "footer", order: 10, semantic: ActionSemantic.Ok, isPrimary: true),
                    CreateAction("help", "header", order: 5, semantic: ActionSemantic.Help),
                    CreateAction("hidden", "footer", order: 1, semantic: ActionSemantic.Secondary, visible: false),
                });

            var plan = builder.Build(document);

            Assert.Multiple(() =>
            {
                Assert.That(plan.Document, Is.SameAs(document));
                Assert.That(plan.Areas.Count, Is.EqualTo(2));

                var header = plan.Areas.Single(area => string.Equals(area.Area.Id, "header", StringComparison.OrdinalIgnoreCase));
                var footer = plan.Areas.Single(area => string.Equals(area.Area.Id, "footer", StringComparison.OrdinalIgnoreCase));

                Assert.That(header.Actions.Select(action => action.Id), Is.EqualTo(new[] { "help" }));
                Assert.That(footer.Actions.Select(action => action.Id), Is.EqualTo(new[] { "save", "cancel" }));
            });
        }

        [Test]
        public void Build_ShouldCreateDefaultArea_ForVisibleActionsWithoutDeclaredArea()
        {
            var builder = new DocumentActionRenderPlanBuilder();
            var document = CreateDocument(
                actionAreas: Array.Empty<ResolvedActionAreaDefinition>(),
                actions: new[]
                {
                    CreateAction("save", null, order: 10, semantic: ActionSemantic.Ok, isPrimary: true),
                    CreateAction("cancel", null, order: 20, semantic: ActionSemantic.Cancel),
                });

            var plan = builder.Build(document);
            var area = plan.Areas.Single();

            Assert.Multiple(() =>
            {
                Assert.That(area.Area.Id, Is.Null);
                Assert.That(area.Area.HorizontalAlignment, Is.EqualTo(ActionAlignment.Right));
                Assert.That(area.Actions.Select(action => action.Id), Is.EqualTo(new[] { "save", "cancel" }));
            });
        }

        private static ResolvedFormDocumentDefinition CreateDocument(
            IReadOnlyList<ResolvedActionAreaDefinition> actionAreas,
            IReadOnlyList<ResolvedDocumentActionDefinition> actions)
        {
            return new ResolvedFormDocumentDefinition(
                id: "doc",
                name: "Document",
                kind: DocumentKind.Form,
                width: null,
                widthHint: FieldWidthHint.Fill,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: false,
                validationTrigger: ValidationTrigger.OnChange,
                interactionMode: InteractionMode.Classic,
                densityMode: DensityMode.Normal,
                fieldChromeMode: FieldChromeMode.Framed,
                captionPlacement: CaptionPlacement.Top,
                layout: null,
                actionAreas: actionAreas,
                fields: Array.Empty<ResolvedFieldDefinition>(),
                actions: actions,
                fieldMap: new Dictionary<string, ResolvedFieldDefinition>(StringComparer.OrdinalIgnoreCase));
        }

        private static ResolvedActionAreaDefinition CreateArea(string id, ActionAlignment alignment)
        {
            return new ResolvedActionAreaDefinition(
                new YamlActionAreaDefinition
                {
                    Id = id,
                    Placement = ActionPlacement.Bottom,
                    HorizontalAlignment = alignment,
                    Shared = true,
                    Sticky = false,
                    Visible = true,
                },
                placement: ActionPlacement.Bottom,
                horizontalAlignment: alignment,
                shared: true,
                sticky: false,
                visible: true);
        }

        private static ResolvedDocumentActionDefinition CreateAction(
            string id,
            string area,
            int? order,
            ActionSemantic semantic,
            bool isPrimary = false,
            bool visible = true)
        {
            return new ResolvedDocumentActionDefinition(
                new YamlDocumentActionDefinition
                {
                    Id = id,
                    CaptionKey = id,
                    Area = area,
                    Order = order,
                    Semantic = semantic,
                    IsPrimary = isPrimary,
                    Visible = visible,
                    Enabled = true,
                },
                visible: visible,
                enabled: true);
        }
    }
}
