using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Core.Normalization
{
    public sealed class YamlDocumentNormalizationResult
    {
        public YamlDocumentNormalizationResult(
            YamlDocumentDefinition document,
            ResolvedDocumentDefinition resolvedDocument,
            IReadOnlyDictionary<string, string> fieldIndex,
            IReadOnlyList<string> diagnostics)
        {
            Document = document;
            ResolvedDocument = resolvedDocument;
            FieldIndex = fieldIndex ?? new Dictionary<string, string>();
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public YamlDocumentDefinition Document { get; }

        public ResolvedDocumentDefinition ResolvedDocument { get; }

        public IReadOnlyDictionary<string, string> FieldIndex { get; }

        public IReadOnlyList<string> Diagnostics { get; }

        public bool Success => Diagnostics.Count == 0 && Document != null && ResolvedDocument != null;
    }
}


