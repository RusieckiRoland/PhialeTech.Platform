using System;
using System.Collections.Generic;

namespace PhialeTech.Components.Shared.Services
{
    public static class ReportDesignerShowcaseTextCatalog
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Languages =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Headline"] = "ReportDesigner demo with native shell",
                    ["Description"] = "This showcase keeps the shell native to the desktop host and runs the browser-hosted report designer on the neutral PhialeTech WebHost.",
                    ["LoadSample"] = "Load sample",
                    ["Expand"] = "Expand demo",
                    ["ExitFocus"] = "Exit focus",
                    ["LoadError"] = "Failed to load the report designer sample.",
                },
                ["pl"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Headline"] = "Demo ReportDesignera z natywnym shellem",
                    ["Description"] = "To demo zachowuje shell po stronie natywnej aplikacji desktopowej i uruchamia browser-hosted designer raportów na neutralnym PhialeTech WebHost.",
                    ["LoadSample"] = "Wczytaj próbkę",
                    ["Expand"] = "Powiększ demo",
                    ["ExitFocus"] = "Wyjdź z trybu focus",
                    ["LoadError"] = "Nie udało się wczytać próbki ReportDesignera.",
                }
            };

        public static string NormalizeLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "en";
            }

            string normalized = languageCode.Trim();
            int separatorIndex = normalized.IndexOf('-');
            if (separatorIndex > 0)
            {
                normalized = normalized.Substring(0, separatorIndex);
            }

            separatorIndex = normalized.IndexOf('_');
            if (separatorIndex > 0)
            {
                normalized = normalized.Substring(0, separatorIndex);
            }

            return Languages.ContainsKey(normalized) ? normalized : "en";
        }

        public static string GetText(string languageCode, string key)
        {
            string normalizedLanguage = NormalizeLanguage(languageCode);
            if (Languages.TryGetValue(normalizedLanguage, out IReadOnlyDictionary<string, string> language) &&
                language.TryGetValue(key, out string localized))
            {
                return localized;
            }

            return Languages["en"].TryGetValue(key, out string fallback)
                ? fallback
                : key;
        }
    }
}
