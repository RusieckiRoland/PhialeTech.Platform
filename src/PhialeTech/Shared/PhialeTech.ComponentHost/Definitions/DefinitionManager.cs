using System;
using System.Collections.Generic;
using System.Linq;
using PhialeTech.ComponentHost.Abstractions.Definitions;

namespace PhialeTech.ComponentHost.Definitions
{
    public sealed class DefinitionManager
    {
        private readonly IReadOnlyList<IDefinitionSource> _sources;

        public DefinitionManager(IEnumerable<IDefinitionSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            _sources = sources
                .Where(source => source != null)
                .ToArray();
        }

        public IReadOnlyList<IDefinitionSource> Sources => _sources;

        public bool TryResolve<TDefinition>(string definitionKey, out DefinitionResolution<TDefinition> resolution)
        {
            var normalizedDefinitionKey = NormalizeDefinitionKey(definitionKey);
            foreach (var source in _sources)
            {
                if (!source.TryResolve(normalizedDefinitionKey, out resolution) || resolution == null)
                {
                    continue;
                }

                return true;
            }

            resolution = null;
            return false;
        }

        public DefinitionResolution<TDefinition> Resolve<TDefinition>(string definitionKey)
        {
            if (TryResolve<TDefinition>(definitionKey, out var resolution))
            {
                return resolution;
            }

            throw new KeyNotFoundException("Definition '" + NormalizeDefinitionKey(definitionKey) + "' was not found.");
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
