using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Resolved;

namespace PhialeTech.YamlApp.Runtime.Model
{
    public sealed class RuntimeDocumentState
    {
        private readonly Dictionary<string, RuntimeFieldState> _fieldMap;

        public RuntimeDocumentState(ResolvedDocumentDefinition document, IReadOnlyList<RuntimeFieldState> fields, IReadOnlyList<RuntimeActionState> actions)
        {
            Document = document;
            Fields = fields ?? Array.Empty<RuntimeFieldState>();
            Actions = actions ?? Array.Empty<RuntimeActionState>();

            _fieldMap = new Dictionary<string, RuntimeFieldState>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in Fields)
            {
                if (field != null && !string.IsNullOrWhiteSpace(field.Id))
                {
                    _fieldMap[field.Id] = field;
                }
            }
        }

        public ResolvedDocumentDefinition Document { get; }

        public string Id => Document == null ? null : Document.Id;

        public string Name => Document == null ? null : Document.Name;

        public DocumentKind? Kind => Document == null ? null : Document.Kind;

        public bool Visible => Document != null && Document.Visible;

        public bool Enabled => Document != null && Document.Enabled;

        public IReadOnlyList<RuntimeFieldState> Fields { get; }

        public IReadOnlyList<RuntimeActionState> Actions { get; }

        public RuntimeFieldState GetField(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                return null;
            }

            RuntimeFieldState field;
            return _fieldMap.TryGetValue(fieldId, out field) ? field : null;
        }
    }
}

