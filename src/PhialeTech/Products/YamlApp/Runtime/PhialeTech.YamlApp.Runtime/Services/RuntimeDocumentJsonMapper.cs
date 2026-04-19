using System;
using System.Collections.Generic;
using System.Text.Json;
using PhialeTech.YamlApp.Abstractions.Results;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Runtime.Services
{
    public sealed class RuntimeDocumentJsonMapper
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public RuntimeDocumentState CreateFromJson(ResolvedFormDocumentDefinition document, string json)
        {
            var factory = new RuntimeDocumentStateFactory();
            var state = factory.Create(document);
            ApplyJson(state, json);
            return state;
        }

        public void ApplyJson(RuntimeDocumentState state, string json)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            using (var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return;
                }

                JsonElement documentElement;
                if (!root.TryGetProperty(state.Id ?? string.Empty, out documentElement) || documentElement.ValueKind != JsonValueKind.Object)
                {
                    return;
                }

                foreach (var property in documentElement.EnumerateObject())
                {
                    var field = state.GetField(property.Name);
                    if (field == null)
                    {
                        continue;
                    }

                    field.LoadValue(ReadValue(property.Value));
                }
            }
        }

        public string ToJson(RuntimeDocumentState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var documentPayload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in state.Fields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.Id))
                {
                    continue;
                }

                documentPayload[field.Id] = field.Value;
            }

            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [state.Id ?? "Document"] = documentPayload
            };

            return JsonSerializer.Serialize(payload, JsonOptions);
        }

        public DynamicDocumentDialogResult ToConfirmedResult(RuntimeDocumentState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return DynamicDocumentDialogResult.Confirmed(state.Id, ToJson(state));
        }

        private static object ReadValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.ToString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return element.GetRawText();
            }
        }
    }
}


