using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhialeGrid.Core.State
{
    public static class GridNamedViewCodec
    {
        private const string Version1Prefix = "v1";

        public static string Encode(IEnumerable<GridNamedViewDefinition> views)
        {
            if (views == null)
            {
                throw new ArgumentNullException(nameof(views));
            }

            var tokens = views
                .Where(view => view != null)
                .OrderBy(view => view.Name, StringComparer.OrdinalIgnoreCase)
                .Select(view => string.Join(";",
                    EncodeText(view.Name),
                    EncodeText(view.GridState)));

            return string.Join("|", new[] { Version1Prefix }.Concat(tokens));
        }

        public static IReadOnlyList<GridNamedViewDefinition> Decode(string encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return Array.Empty<GridNamedViewDefinition>();
            }

            var parts = encoded.Split('|');
            var entries = parts.Length > 0 && string.Equals(parts[0], Version1Prefix, StringComparison.Ordinal)
                ? parts.Skip(1)
                : parts.AsEnumerable();

            return entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Split(';'))
                .Where(entry => entry.Length >= 2)
                .Select(entry => new GridNamedViewDefinition(
                    DecodeText(entry[0]),
                    DecodeText(entry[1])))
                .OrderBy(view => view.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string EncodeText(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            return Convert.ToBase64String(bytes);
        }

        private static string DecodeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
