using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Core.Rendering
{
    public sealed class DocumentActionRenderPlanBuilder
    {
        public DocumentActionRenderPlan Build(ResolvedFormDocumentDefinition document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var visibleActions = (document.Actions ?? Array.Empty<ResolvedDocumentActionDefinition>())
                .Where(action => action != null && action.Visible)
                .ToList();

            var actionsByArea = visibleActions
                .GroupBy(action => NormalizeAreaId(action.Area), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ResolvedDocumentActionDefinition>)group
                        .OrderBy(action => action.Order ?? int.MaxValue)
                        .ThenBy(action => action.Id, StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var areas = new List<DocumentActionAreaRenderPlan>();
            var knownAreaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var area in document.ActionAreas ?? Array.Empty<ResolvedActionAreaDefinition>())
            {
                if (area == null || !area.Visible || string.IsNullOrWhiteSpace(area.Id))
                {
                    continue;
                }

                knownAreaIds.Add(area.Id);
                actionsByArea.TryGetValue(area.Id, out var areaActions);
                areas.Add(new DocumentActionAreaRenderPlan(area, areaActions ?? Array.Empty<ResolvedDocumentActionDefinition>()));
            }

            if (actionsByArea.TryGetValue(string.Empty, out var defaultActions) && defaultActions.Count > 0)
            {
                areas.Add(new DocumentActionAreaRenderPlan(
                    new ResolvedActionAreaDefinition(
                        definition: null,
                        placement: Abstractions.Enums.ActionPlacement.Bottom,
                        horizontalAlignment: Abstractions.Enums.ActionAlignment.Right,
                        shared: true,
                        sticky: false,
                        visible: true),
                    defaultActions));
            }

            foreach (var pair in actionsByArea)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || knownAreaIds.Contains(pair.Key) || pair.Value.Count == 0)
                {
                    continue;
                }

                areas.Add(new DocumentActionAreaRenderPlan(
                    new ResolvedActionAreaDefinition(
                        definition: null,
                        placement: Abstractions.Enums.ActionPlacement.Bottom,
                        horizontalAlignment: Abstractions.Enums.ActionAlignment.Right,
                        shared: false,
                        sticky: false,
                        visible: true),
                    pair.Value));
            }

            return new DocumentActionRenderPlan(document, areas);
        }

        private static string NormalizeAreaId(string areaId)
        {
            return string.IsNullOrWhiteSpace(areaId)
                ? string.Empty
                : areaId.Trim();
        }
    }
}
