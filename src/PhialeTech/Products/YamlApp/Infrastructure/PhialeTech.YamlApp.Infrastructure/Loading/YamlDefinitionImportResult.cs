using System;
using System.Collections.Generic;

namespace PhialeTech.YamlApp.Infrastructure.Loading
{
    public sealed class YamlDefinitionImportResult<TDefinition>
    {
        public YamlDefinitionImportResult(TDefinition definition, IReadOnlyList<string> diagnostics)
        {
            Definition = definition;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public TDefinition Definition { get; }

        public IReadOnlyList<string> Diagnostics { get; }

        public bool Success => Diagnostics.Count == 0 && Definition != null;
    }
}
