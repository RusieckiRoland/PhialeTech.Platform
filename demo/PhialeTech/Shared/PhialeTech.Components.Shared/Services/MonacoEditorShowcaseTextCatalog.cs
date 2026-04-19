using System;
using System.Collections.Generic;

namespace PhialeTech.Components.Shared.Services
{
    public static class MonacoEditorShowcaseTextCatalog
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Languages =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Headline"] = "MonacoEditor demo with neutral web host",
                    ["Description"] = "This showcase keeps the native WPF shell outside and mounts Monaco inside the shared PhialeTech WebHost.",
                    ["LoadYaml"] = "Load YAML",
                    ["LoadCSharp"] = "Load C#",
                    ["Focus"] = "Focus editor",
                    ["Expand"] = "Expand demo",
                    ["ExitFocus"] = "Exit focus",
                    ["LoadError"] = "Failed to load the MonacoEditor sample.",
                },
                ["pl"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Headline"] = "Demo MonacoEditora na neutralnym web hoście",
                    ["Description"] = "To demo zachowuje natywny shell po stronie WPF i osadza Monaco we wspólnym PhialeTech WebHost.",
                    ["LoadYaml"] = "Wczytaj YAML",
                    ["LoadCSharp"] = "Wczytaj C#",
                    ["Focus"] = "Ustaw fokus",
                    ["Expand"] = "Powiększ demo",
                    ["ExitFocus"] = "Wyjdź z trybu focus",
                    ["LoadError"] = "Nie udało się wczytać próbki MonacoEditora.",
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
