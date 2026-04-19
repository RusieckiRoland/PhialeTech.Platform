using System;
using System.Collections.Generic;
using PhialeGrid.Core.Export;

namespace PhialeGrid.Core.Clipboard
{
    public static class GridClipboardCodec
    {
        public static string Encode(IReadOnlyList<IReadOnlyList<string>> rows)
        {
            return Encode(rows, new GridClipboardOptions());
        }

        public static string Encode(IReadOnlyList<IReadOnlyList<string>> rows, GridClipboardOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return DelimitedTextCodec.Encode(rows, options.Delimiter, options.LineEnding);
        }

        public static IReadOnlyList<IReadOnlyList<string>> Decode(string text)
        {
            return Decode(text, new GridClipboardOptions());
        }

        public static IReadOnlyList<IReadOnlyList<string>> Decode(string text, GridClipboardOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return DelimitedTextCodec.Decode(text, options.Delimiter);
        }
    }
}
