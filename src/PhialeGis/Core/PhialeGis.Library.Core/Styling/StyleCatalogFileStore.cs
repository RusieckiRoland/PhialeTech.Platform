using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class StyleCatalogFileStore
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private readonly StyleCatalogSerializer _serializer;

        public StyleCatalogFileStore(StyleCatalogSerializer serializer = null)
        {
            _serializer = serializer ?? new StyleCatalogSerializer();
        }

        public void SaveSymbols(string path, ISymbolCatalog catalog)
        {
            Save(path, _serializer.SerializeSymbols(catalog));
        }

        public void SaveLineTypes(string path, ILineTypeCatalog catalog)
        {
            Save(path, _serializer.SerializeLineTypes(catalog));
        }

        public void SaveFillStyles(string path, IFillStyleCatalog catalog)
        {
            Save(path, _serializer.SerializeFillStyles(catalog));
        }

        public IReadOnlyList<SymbolDefinition> LoadSymbols(string path)
        {
            return _serializer.DeserializeSymbols(File.ReadAllText(ValidatePath(path), Utf8WithoutBom));
        }

        public IReadOnlyList<LineTypeDefinition> LoadLineTypes(string path)
        {
            return _serializer.DeserializeLineTypes(File.ReadAllText(ValidatePath(path), Utf8WithoutBom));
        }

        public IReadOnlyList<FillStyleDefinition> LoadFillStyles(string path)
        {
            return _serializer.DeserializeFillStyles(File.ReadAllText(ValidatePath(path), Utf8WithoutBom));
        }

        private static void Save(string path, string payload)
        {
            var validatedPath = ValidatePath(path);
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(validatedPath, payload, Utf8WithoutBom);
        }

        private static string ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path cannot be empty.", nameof(path));

            return path;
        }
    }
}
