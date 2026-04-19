using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoResolvedDefinitionViewModel
    {
        public DemoResolvedDefinitionViewModel(
            string definitionKey,
            string sourceId,
            string definitionKind,
            string componentId,
            string title,
            string summary,
            string consumerHint,
            string stateOverlayHint,
            IEnumerable<string> responsibilities,
            IEnumerable<string> outOfScope,
            IEnumerable<DemoDefinitionField> fields)
        {
            DefinitionKey = definitionKey ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            DefinitionKind = definitionKind ?? string.Empty;
            ComponentId = componentId ?? string.Empty;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            ConsumerHint = consumerHint ?? string.Empty;
            StateOverlayHint = stateOverlayHint ?? string.Empty;
            Responsibilities = (responsibilities ?? Array.Empty<string>()).ToArray();
            OutOfScope = (outOfScope ?? Array.Empty<string>()).ToArray();
            Fields = (fields ?? Array.Empty<DemoDefinitionField>()).ToArray();
        }

        public string DefinitionKey { get; }

        public string SourceId { get; }

        public string DefinitionKind { get; }

        public string ComponentId { get; }

        public string Title { get; }

        public string Summary { get; }

        public string ConsumerHint { get; }

        public string StateOverlayHint { get; }

        public IReadOnlyList<string> Responsibilities { get; }

        public IReadOnlyList<string> OutOfScope { get; }

        public IReadOnlyList<DemoDefinitionField> Fields { get; }
    }
}
