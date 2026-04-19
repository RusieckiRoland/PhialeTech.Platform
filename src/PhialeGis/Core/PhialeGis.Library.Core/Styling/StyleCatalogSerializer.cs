using System;
using System.Collections.Generic;
using System.Text.Json;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class StyleCatalogSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public string SerializeSymbols(ISymbolCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            return JsonSerializer.Serialize(
                new StyleCatalogEnvelope<SymbolDefinition>
                {
                    SchemaVersion = StyleCatalogSchemaVersion.Current,
                    Items = ToArray(StyleDefinitionCloner.CloneMany(catalog.GetAll()))
                },
                JsonOptions);
        }

        public string SerializeLineTypes(ILineTypeCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            return JsonSerializer.Serialize(
                new StyleCatalogEnvelope<LineTypeDefinition>
                {
                    SchemaVersion = StyleCatalogSchemaVersion.Current,
                    Items = ToArray(StyleDefinitionCloner.CloneMany(catalog.GetAll()))
                },
                JsonOptions);
        }

        public string SerializeFillStyles(IFillStyleCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            return JsonSerializer.Serialize(
                new StyleCatalogEnvelope<FillStyleDefinition>
                {
                    SchemaVersion = StyleCatalogSchemaVersion.Current,
                    Items = ToArray(StyleDefinitionCloner.CloneMany(catalog.GetAll()))
                },
                JsonOptions);
        }

        public IReadOnlyList<SymbolDefinition> DeserializeSymbols(string json)
        {
            var envelope = DeserializeEnvelope<SymbolDefinition>(json);
            return envelope.Items ?? Array.Empty<SymbolDefinition>();
        }

        public IReadOnlyList<LineTypeDefinition> DeserializeLineTypes(string json)
        {
            var envelope = DeserializeEnvelope<LineTypeDefinition>(json);
            return envelope.Items ?? Array.Empty<LineTypeDefinition>();
        }

        public IReadOnlyList<FillStyleDefinition> DeserializeFillStyles(string json)
        {
            var envelope = DeserializeEnvelope<FillStyleDefinition>(json);
            return envelope.Items ?? Array.Empty<FillStyleDefinition>();
        }

        private static StyleCatalogEnvelope<T> DeserializeEnvelope<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Serialized catalog payload cannot be empty.", nameof(json));

            var envelope = JsonSerializer.Deserialize<StyleCatalogEnvelope<T>>(json, JsonOptions);
            if (envelope == null)
                throw new InvalidOperationException("Serialized catalog payload could not be deserialized.");

            if (envelope.SchemaVersion != StyleCatalogSchemaVersion.Current)
            {
                throw new NotSupportedException(
                    $"Unsupported style catalog schema version '{envelope.SchemaVersion}'. Expected '{StyleCatalogSchemaVersion.Current}'.");
            }

            return envelope;
        }

        private static T[] ToArray<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                return Array.Empty<T>();

            var result = new T[items.Count];
            for (int i = 0; i < items.Count; i++)
                result[i] = items[i];

            return result;
        }

        private sealed class StyleCatalogEnvelope<T>
        {
            public int SchemaVersion { get; set; }

            public T[] Items { get; set; } = Array.Empty<T>();
        }
    }
}
