using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Export
{
    public static class GridCsvImporter
    {
        public static IReadOnlyList<IReadOnlyDictionary<string, string>> Import(string csv, bool hasHeader = true)
        {
            return Import(csv, new GridCsvOptions { HasHeaderOnImport = hasHeader });
        }

        public static IReadOnlyList<IReadOnlyDictionary<string, string>> Import(string csv, GridCsvOptions options)
        {
            if (csv == null)
            {
                throw new ArgumentNullException(nameof(csv));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var rows = DelimitedTextCodec.Decode(csv, options.Delimiter);
            if (rows.Count == 0)
            {
                return Array.Empty<IReadOnlyDictionary<string, string>>();
            }

            string[] headers;
            var start = 0;
            if (options.HasHeaderOnImport)
            {
                headers = rows[0].ToArray();
                start = 1;
            }
            else
            {
                headers = Enumerable.Range(0, rows[0].Count).Select(i => "Column" + i).ToArray();
            }

            var result = new List<IReadOnlyDictionary<string, string>>();
            for (var i = start; i < rows.Count; i++)
            {
                var dict = new Dictionary<string, string>();
                for (var col = 0; col < headers.Length; col++)
                {
                    dict[headers[col]] = col < rows[i].Count ? rows[i][col] : string.Empty;
                }

                result.Add(dict);
            }

            return result;
        }
    }
}
