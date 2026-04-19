using System;
using System.Collections.Generic;

namespace PhialeTech.YamlApp.Wpf.Controls
{
    internal static class YamlIconGlyphResolver
    {
        private static readonly IReadOnlyDictionary<string, string> Glyphs =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["apply"] = "\uE73E",
                ["back"] = "\uE72B",
                ["cancel"] = "\uE711",
                ["danger"] = "\uEA39",
                ["document"] = "\uE8A5",
                ["draft"] = "\uE70F",
                ["help"] = "\uE897",
                ["history"] = "\uE81C",
                ["info"] = "\uE946",
                ["internal-form"] = "\uE8D2",
                ["ok"] = "\uE73E",
                ["preview"] = "\uE890",
                ["review"] = "\uE9D5",
                ["save"] = "\uE74E",
                ["save-draft"] = "\uE74E",
                ["validate"] = "\uE73E",
                ["warning"] = "\uE7BA",
            };

        public static string ResolveGlyph(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey))
            {
                return string.Empty;
            }

            if (iconKey.StartsWith("glyph:", StringComparison.OrdinalIgnoreCase))
            {
                return iconKey.Substring("glyph:".Length);
            }

            return Glyphs.TryGetValue(iconKey.Trim(), out var glyph) ? glyph : string.Empty;
        }
    }
}
