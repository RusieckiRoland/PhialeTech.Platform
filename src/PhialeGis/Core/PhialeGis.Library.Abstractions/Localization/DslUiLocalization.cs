using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PhialeGis.Library.Abstractions.Localization
{
    /// <summary>
    /// Shared localization provider used by both DSL completion and interactive actions.
    /// Fallback order for completions: selected language -> en -> raw token in caller.
    /// </summary>
    public static class DslUiLocalization
    {
        private const string DefaultLanguage = "en";
        private const string ResourcePrefix = "PhialeGis.Library.Abstractions.Localization.points.";

        private static readonly ConcurrentDictionary<string, LanguageBundle> Cache =
            new ConcurrentDictionary<string, LanguageBundle>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Application-level selected language. Can be set at app startup.
        /// </summary>
        public static string CurrentLanguageId { get; set; } = DefaultLanguage;

        /// <summary>
        /// Maps arbitrary language id to a supported key.
        /// Examples: "pl-PL" -> "pl", "en-US" -> "en".
        /// </summary>
        public static string NormalizeLanguageId(string languageId)
        {
            var raw = string.IsNullOrWhiteSpace(languageId) ? CurrentLanguageId : languageId;
            if (string.IsNullOrWhiteSpace(raw)) return DefaultLanguage;

            var v = raw.Trim();
            if (v.StartsWith("pl", StringComparison.OrdinalIgnoreCase)) return "pl";
            if (v.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en";
            return v.ToLowerInvariant();
        }

        public static LocalizedCompletion TryGetCompletion(string tokenName, string languageId)
        {
            if (string.IsNullOrWhiteSpace(tokenName))
                return null;

            var lang = NormalizeLanguageId(languageId);
            LocalizedCompletion item;

            if (TryResolveCompletion(lang, tokenName, out item))
                return item;

            if (!string.Equals(lang, DefaultLanguage, StringComparison.OrdinalIgnoreCase) &&
                TryResolveCompletion(DefaultLanguage, tokenName, out item))
            {
                return item;
            }

            return null;
        }

        public static string GetText(string key, string languageId, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            var lang = NormalizeLanguageId(languageId);
            string value;

            if (TryResolveText(lang, key, out value))
                return value;

            if (!string.Equals(lang, DefaultLanguage, StringComparison.OrdinalIgnoreCase) &&
                TryResolveText(DefaultLanguage, key, out value))
            {
                return value;
            }

            return fallback ?? string.Empty;
        }

        private static bool TryResolveCompletion(string language, string tokenName, out LocalizedCompletion item)
        {
            item = null;

            var bundle = GetBundle(language);
            if (bundle == null)
                return false;

            string label;
            if (!bundle.Values.TryGetValue("completion." + tokenName + ".label", out label))
                return false;

            string insert;
            if (!bundle.Values.TryGetValue("completion." + tokenName + ".insert", out insert))
                insert = label;

            string kind;
            if (!bundle.Values.TryGetValue("completion." + tokenName + ".kind", out kind))
                kind = "text";

            item = new LocalizedCompletion
            {
                Label = label ?? string.Empty,
                InsertText = insert ?? string.Empty,
                Kind = string.IsNullOrWhiteSpace(kind) ? "text" : kind
            };
            return true;
        }

        private static bool TryResolveText(string language, string key, out string value)
        {
            value = null;
            var bundle = GetBundle(language);
            if (bundle == null)
                return false;

            return bundle.Values.TryGetValue(key, out value);
        }

        private static LanguageBundle GetBundle(string language)
        {
            var lang = string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language;
            return Cache.GetOrAdd(lang, LoadBundle);
        }

        private static LanguageBundle LoadBundle(string language)
        {
            var asm = typeof(DslUiLocalization).GetTypeInfo().Assembly;
            var resourceName = FindResourceName(asm, language);
            if (string.IsNullOrWhiteSpace(resourceName))
                return LanguageBundle.Empty;

            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return LanguageBundle.Empty;
                using (var reader = new StreamReader(stream))
                {
                    var values = ParseKeyValue(reader);
                    return new LanguageBundle(values);
                }
            }
        }

        private static string FindResourceName(Assembly asm, string language)
        {
            var suffix = "." + language + ".ini";
            var names = asm.GetManifestResourceNames();
            if (names == null || names.Length == 0)
                return null;

            for (int i = 0; i < names.Length; i++)
            {
                var n = names[i];
                if (!n.StartsWith(ResourcePrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return n;
            }

            return null;
        }

        private static Dictionary<string, string> ParseKeyValue(StreamReader reader)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (trimmed.StartsWith("#", StringComparison.Ordinal)) continue;

                var idx = trimmed.IndexOf('=');
                if (idx <= 0) continue;

                var key = trimmed.Substring(0, idx).Trim();
                if (key.Length == 0) continue;

                var value = trimmed.Substring(idx + 1).Trim();
                map[key] = value;
            }

            return map;
        }

        private sealed class LanguageBundle
        {
            internal static readonly LanguageBundle Empty = new LanguageBundle(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            internal LanguageBundle(Dictionary<string, string> values)
            {
                Values = values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            internal Dictionary<string, string> Values { get; private set; }
        }
    }

    public sealed class LocalizedCompletion
    {
        public string Label { get; set; } = string.Empty;
        public string InsertText { get; set; } = string.Empty;
        public string Kind { get; set; } = "text";
    }
}
