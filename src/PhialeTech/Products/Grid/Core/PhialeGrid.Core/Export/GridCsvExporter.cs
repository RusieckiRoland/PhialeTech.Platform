using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhialeGrid.Core.Export
{
    public static class GridCsvExporter
    {
        public static string Export<T>(IReadOnlyList<T> rows, IReadOnlyList<string> columns, Func<T, string, object> valueSelector, bool includeHeader = true)
        {
            return Export(rows, columns, valueSelector, new GridCsvOptions { IncludeHeader = includeHeader });
        }

        public static string Export<T>(IReadOnlyList<T> rows, IReadOnlyList<string> columns, Func<T, string, object> valueSelector, GridCsvOptions options)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var matrix = new List<IReadOnlyList<string>>();
            if (options.IncludeHeader)
            {
                matrix.Add(columns.Select(c => c ?? string.Empty).ToArray());
            }

            foreach (var row in rows)
            {
                matrix.Add(columns
                    .Select(c => Convert.ToString(valueSelector(row, c), CultureInfo.InvariantCulture) ?? string.Empty)
                    .ToArray());
            }

            return DelimitedTextCodec.Encode(matrix, options.Delimiter, options.LineEnding);
        }
    }
}
