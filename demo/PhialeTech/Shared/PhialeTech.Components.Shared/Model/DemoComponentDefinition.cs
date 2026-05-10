using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoComponentDefinition
    {
        public DemoComponentDefinition(
            string definitionKind,
            string componentId,
            string titleKey,
            string summaryKey,
            string consumerHintKey,
            string stateOverlayHintKey,
            IEnumerable<string> responsibilityTextKeys = null,
            IEnumerable<string> outOfScopeTextKeys = null,
            IEnumerable<DemoDefinitionField> fields = null)
        {
            DefinitionKind = string.IsNullOrWhiteSpace(definitionKind) ? "screen" : definitionKind.Trim();
            ComponentId = componentId ?? string.Empty;
            TitleKey = titleKey ?? string.Empty;
            SummaryKey = summaryKey ?? string.Empty;
            ConsumerHintKey = consumerHintKey ?? string.Empty;
            StateOverlayHintKey = stateOverlayHintKey ?? string.Empty;
            ResponsibilityTextKeys = (responsibilityTextKeys ?? Array.Empty<string>()).ToArray();
            OutOfScopeTextKeys = (outOfScopeTextKeys ?? Array.Empty<string>()).ToArray();
            Fields = (fields ?? Array.Empty<DemoDefinitionField>()).ToArray();
        }

        public string DefinitionKind { get; }

        public string ComponentId { get; }

        public string TitleKey { get; }

        public string SummaryKey { get; }

        public string ConsumerHintKey { get; }

        public string StateOverlayHintKey { get; }

        public IReadOnlyList<string> ResponsibilityTextKeys { get; }

        public IReadOnlyList<string> OutOfScopeTextKeys { get; }

        public IReadOnlyList<DemoDefinitionField> Fields { get; }
    }
}

