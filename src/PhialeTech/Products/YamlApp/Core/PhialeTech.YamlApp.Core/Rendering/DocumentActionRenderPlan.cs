using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Core.Rendering
{
    public sealed class DocumentActionRenderPlan
    {
        public DocumentActionRenderPlan(
            ResolvedFormDocumentDefinition document,
            IReadOnlyList<DocumentActionAreaRenderPlan> areas)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Areas = areas ?? Array.Empty<DocumentActionAreaRenderPlan>();
        }

        public ResolvedFormDocumentDefinition Document { get; }

        public IReadOnlyList<DocumentActionAreaRenderPlan> Areas { get; }
    }
}
