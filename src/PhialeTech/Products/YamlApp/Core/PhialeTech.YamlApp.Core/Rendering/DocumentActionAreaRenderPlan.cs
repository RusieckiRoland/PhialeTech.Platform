using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Core.Rendering
{
    public sealed class DocumentActionAreaRenderPlan
    {
        public DocumentActionAreaRenderPlan(
            ResolvedActionAreaDefinition area,
            IReadOnlyList<ResolvedDocumentActionDefinition> actions)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Actions = actions ?? Array.Empty<ResolvedDocumentActionDefinition>();
        }

        public ResolvedActionAreaDefinition Area { get; }

        public IReadOnlyList<ResolvedDocumentActionDefinition> Actions { get; }
    }
}
