using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhialeGrid.Core.Export
{
    internal static class DelimitedTextCodec
    {
        public static string Encode(IReadOnlyList<IReadOnlyList<string>> rows, char delimiter, string lineEnding)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            return string.Join(lineEnding ?? "\n", rows.Select(row => string.Join(delimiter.ToString(), row.Select(value => Escape(value, delimiter)))));
        }

        public static IReadOnlyList<IReadOnlyList<string>> Decode(string text, char delimiter)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length == 0)
            {
                return Array.Empty<IReadOnlyList<string>>();
            }

            var rows = new List<IReadOnlyList<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            currentCell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentCell.Append(ch);
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                    continue;
                }

                if (ch == delimiter)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if (ch == '\r' || ch == '\n')
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    rows.Add(currentRow.ToArray());
                    currentRow = new List<string>();

                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    continue;
                }

                currentCell.Append(ch);
            }

            currentRow.Add(currentCell.ToString());
            rows.Add(currentRow.ToArray());
            return rows;
        }

        private static string Escape(string value, char delimiter)
        {
            var text = value ?? string.Empty;
            if (text.IndexOfAny(new[] { delimiter, '"', '\n', '\r' }) < 0)
            {
                return text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
