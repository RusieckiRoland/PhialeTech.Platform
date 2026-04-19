using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhialeTech.ActiveLayerSelector.Localization
{
    public sealed class ActiveLayerSelectorLocalizationCatalog
    {
        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _languages;

        public static ActiveLayerSelectorLocalizationCatalog Empty { get; } =
            new ActiveLayerSelectorLocalizationCatalog(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase));

        public ActiveLayerSelectorLocalizationCatalog(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> languages, string fallbackLanguageCode = "en")
        {
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
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

            FallbackLanguageCode = string.IsNullOrWhiteSpace(fallbackLanguageCode) ? "en" : fallbackLanguageCode;
        }

        public string FallbackLanguageCode { get; }

        public static ActiveLayerSelectorLocalizationCatalog LoadDefault(string fallbackLanguageCode = "en")
        {
            return LoadFromDirectory(GetDefaultLanguageDirectory(), fallbackLanguageCode);
        }

        public static ActiveLayerSelectorLocalizationCatalog LoadFromDirectory(string directoryPath, string fallbackLanguageCode = "en")
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
                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    languages[languageCode] = ParseLanguageFile(filePath);
                }
            }

            return new ActiveLayerSelectorLocalizationCatalog(languages, fallbackLanguageCode);
        }

        public static string GetDefaultLanguageDirectory()
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(ActiveLayerSelectorLocalizationCatalog).Assembly.Location) ?? AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(assemblyDirectory, "PhialeTech.ActiveLayerSelector", "Languages"),
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

            var normalizedLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim();
            if (TryGetText(normalizedLanguageCode, key, out var value))
            {
                return value;
            }

            if (!string.Equals(normalizedLanguageCode, FallbackLanguageCode, StringComparison.OrdinalIgnoreCase) && TryGetText(FallbackLanguageCode, key, out var fallback))
            {
                return fallback;
            }

            return key;
        }

        private bool TryGetText(string languageCode, string key, out string value)
        {
            value = null;
            if (_languages.TryGetValue(languageCode, out var language) && language.TryGetValue(key, out var translated))
            {
                value = translated;
                return true;
            }

            var separatorIndex = languageCode.IndexOf('-');
            if (separatorIndex > 0)
            {
                var neutralLanguageCode = languageCode.Substring(0, separatorIndex);
                if (_languages.TryGetValue(neutralLanguageCode, out var neutralLanguage) && neutralLanguage.TryGetValue(key, out translated))
                {
                    value = translated;
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyDictionary<string, string> ParseLanguageFile(string filePath)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
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
                entries[Unescape(key)] = Unescape(value);
            }

            return entries;
        }

        private static int FindSeparator(string line)
        {
            var escaped = false;
            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (character == '=')
                {
                    return index;
                }
            }

            return -1;
        }

        private static string Unescape(string value)
        {
            return value.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\=", "=").Replace("\\\\", "\\");
        }
    }
}

