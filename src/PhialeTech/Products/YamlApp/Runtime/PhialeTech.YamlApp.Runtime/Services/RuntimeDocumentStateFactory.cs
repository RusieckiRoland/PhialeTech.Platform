using System;
using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Runtime.Services
{
    public sealed class RuntimeDocumentStateFactory
    {
        public RuntimeDocumentState Create(ResolvedFormDocumentDefinition form)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            var fields = new List<RuntimeFieldState>();
            foreach (var field in form.Fields)
            {
                var runtimeField = new RuntimeFieldState(field);
                var documentEditorField = field.Definition as IDocumentEditorFieldDefinition;
                if (documentEditorField != null && !string.IsNullOrWhiteSpace(documentEditorField.Value))
                {
                    runtimeField.LoadValue(documentEditorField.Value);
                }

                fields.Add(runtimeField);
            }

            var actions = new List<RuntimeActionState>();
            foreach (var action in form.Actions)
            {
                actions.Add(new RuntimeActionState(action));
            }

            return new RuntimeDocumentState(form, fields, actions);
        }
    }
}

