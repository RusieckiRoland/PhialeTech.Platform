using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhialeGrid.Localization
{
    public sealed class GridLocalizationCatalog
    {
        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _languages;

        public static GridLocalizationCatalog Empty { get; } =
            new GridLocalizationCatalog(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase));

        public GridLocalizationCatalog(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> languages,
            string fallbackLanguageCode = "en")
        {
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }

            if (string.IsNullOrWhiteSpace(fallbackLanguageCode))
            {
                throw new ArgumentException("Fallback language code is required.", nameof(fallbackLanguageCode));
            }

            _languages = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in languages)
            {
                var copiedLanguage = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var entry in pair.Value)
                {
                    copiedLanguage[entry.Key] = entry.Value;
                }

                _languages[pair.Key] = copiedLanguage;
            }

            FallbackLanguageCode = fallbackLanguageCode;
        }

        public string FallbackLanguageCode { get; }

        public IReadOnlyCollection<string> AvailableLanguageCodes => _languages.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        public static GridLocalizationCatalog LoadFromDirectory(string directoryPath, string fallbackLanguageCode = "en")
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Language directory path is required.", nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException("Language directory not found: " + directoryPath);
            }

            var languages = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var filePath in Directory.GetFiles(directoryPath, "*.lang", SearchOption.TopDirectoryOnly))
            {
                var languageCode = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(languageCode))
                {
                    continue;
                }

                languages[languageCode] = ParseLanguageFile(filePath);
            }

            return new GridLocalizationCatalog(languages, fallbackLanguageCode);
        }

        public static GridLocalizationCatalog LoadDefault(string fallbackLanguageCode = "en")
        {
            return LoadFromDirectory(GetDefaultLanguageDirectory(), fallbackLanguageCode);
        }

        public static string GetDefaultLanguageDirectory()
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(GridLocalizationCatalog).Assembly.Location) ?? AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(assemblyDirectory, "PhialeGrid.Localization", "Languages"),
                Path.Combine(assemblyDirectory, "Languages"),
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        public string GetText(string languageCode, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Text key is required.", nameof(key));
            }

            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);

            if (TryGetText(normalizedLanguageCode, key, out var localizedText))
            {
                return localizedText;
            }

            if (!string.Equals(normalizedLanguageCode, FallbackLanguageCode, StringComparison.OrdinalIgnoreCase) &&
                TryGetText(FallbackLanguageCode, key, out var fallbackText))
            {
                return fallbackText;
            }

            return key;
        }

        public IReadOnlyDictionary<string, string> GetLanguage(string languageCode)
        {
            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            if (_languages.TryGetValue(normalizedLanguageCode, out var language))
            {
                return language;
            }

            if (_languages.TryGetValue(FallbackLanguageCode, out var fallback))
            {
                return fallback;
            }

            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "en";
            }

            return languageCode.Trim();
        }

        private bool TryGetText(string languageCode, string key, out string value)
        {
            value = null;
            if (_languages.TryGetValue(languageCode, out var language) && language.TryGetValue(key, out var localized))
            {
                value = localized;
                return true;
            }

            var separatorIndex = languageCode.IndexOf('-');
            if (separatorIndex > 0)
            {
                var neutralLanguage = languageCode.Substring(0, separatorIndex);
                if (_languages.TryGetValue(neutralLanguage, out var neutral) && neutral.TryGetValue(key, out localized))
                {
                    value = localized;
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyDictionary<string, string> ParseLanguageFile(string filePath)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = FindSeparator(line);
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1);
                result[Unescape(key)] = Unescape(value);
            }

            return result;
        }

        private static int FindSeparator(string line)
        {
            var escaped = false;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '=')
                {
                    return i;
                }
            }

            return -1;
        }

        private static string Unescape(string value)
        {
            return value
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\=", "=")
                .Replace("\\\\", "\\");
        }
    }
}
