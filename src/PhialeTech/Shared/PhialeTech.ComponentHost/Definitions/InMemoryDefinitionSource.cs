using System;
using System.Collections.Generic;
using PhialeTech.ComponentHost.Abstractions.Definitions;

namespace PhialeTech.ComponentHost.Definitions
{
    public sealed class InMemoryDefinitionSource : IDefinitionSource
    {
        private readonly Dictionary<string, object> _definitions = new Dictionary<string, object>(StringComparer.Ordinal);

        public InMemoryDefinitionSource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source id is required.", nameof(sourceId));
            }

            SourceId = sourceId.Trim();
        }

        public string SourceId { get; }

        public InMemoryDefinitionSource Add<TDefinition>(string definitionKey, TDefinition definition)
        {
            var normalizedDefinitionKey = NormalizeDefinitionKey(definitionKey);
            _definitions[normalizedDefinitionKey] = definition;
            return this;
        }

        public bool TryResolve<TDefinition>(string definitionKey, out DefinitionResolution<TDefinition> resolution)
        {
            var normalizedDefinitionKey = NormalizeDefinitionKey(definitionKey);
            if (_definitions.TryGetValue(normalizedDefinitionKey, out var definition) && definition is TDefinition typedDefinition)
            {
                resolution = new DefinitionResolution<TDefinition>(normalizedDefinitionKey, SourceId, typedDefinition);
                return true;
            }

            resolution = null;
            return false;
        }

        private static string NormalizeDefinitionKey(string definitionKey)
        {
            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                throw new ArgumentException("Definition key is required.", nameof(definitionKey));
            }

            return definitionKey.Trim();
        }
    }
}
