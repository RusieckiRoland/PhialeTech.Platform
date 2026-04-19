using System;

namespace PhialeTech.ComponentHost.Abstractions.Definitions
{
    public sealed class DefinitionResolution<TDefinition>
    {
        public DefinitionResolution(string definitionKey, string sourceId, TDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                throw new ArgumentException("Definition key is required.", nameof(definitionKey));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source id is required.", nameof(sourceId));
            }

            DefinitionKey = definitionKey.Trim();
            SourceId = sourceId.Trim();
            Definition = definition;
        }

        public string DefinitionKey { get; }

        public string SourceId { get; }

        public TDefinition Definition { get; }
    }
}
