using System;
using System.Collections.Generic;

namespace PhialeTech.ReportDesigner
{
    public static class ReportDesignerShellTextCatalog
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Languages =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Button.Design"] = "Design",
                    ["Button.Preview"] = "Preview",
                    ["Button.RefreshPreview"] = "Refresh preview",
                    ["Button.Print"] = "Print",
                    ["Button.Focus"] = "Focus",
                    ["Status.Waiting"] = "Status: waiting for browser host",
                    ["Status.HostInitialized"] = "Status: browser host initialized",
                    ["Status.Ready"] = "Status: report designer ready",
                    ["Status.DefinitionChanged"] = "Status: definition updated ({0} root blocks)",
                    ["Status.PreviewReady"] = "Status: preview ready ({0} pages{1})",
                    ["Status.PreviewSource.Sample"] = ", sample data",
                    ["Status.PreviewSource.Report"] = ", report data",
                    ["Status.Mode.Design"] = "Status: design mode",
                    ["Status.Mode.Preview"] = "Status: preview mode",
                    ["Status.ErrorPrefix"] = "Status: ",
                    ["Error.Initialize"] = "Failed to initialize the ReportDesigner.",
                    ["Error.SwitchDesign"] = "Failed to switch to design mode.",
                    ["Error.SwitchPreview"] = "Failed to switch to preview mode.",
                    ["Error.RefreshPreview"] = "Failed to refresh report preview.",
                    ["Error.Print"] = "Failed to start print flow.",
                },
                ["pl"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Button.Design"] = "Projekt",
                    ["Button.Preview"] = "Podgląd",
                    ["Button.RefreshPreview"] = "Odśwież podgląd",
                    ["Button.Print"] = "Drukuj",
                    ["Button.Focus"] = "Fokus",
                    ["Status.Waiting"] = "Status: oczekiwanie na host przeglądarki",
                    ["Status.HostInitialized"] = "Status: host przeglądarki zainicjalizowany",
                    ["Status.Ready"] = "Status: designer raportów gotowy",
                    ["Status.DefinitionChanged"] = "Status: definicja zaktualizowana ({0} bloków głównych)",
                    ["Status.PreviewReady"] = "Status: podgląd gotowy ({0} stron{1})",
                    ["Status.PreviewSource.Sample"] = ", dane przykładowe",
                    ["Status.PreviewSource.Report"] = ", dane raportowe",
                    ["Status.Mode.Design"] = "Status: tryb projektowania",
                    ["Status.Mode.Preview"] = "Status: tryb podglądu",
                    ["Status.ErrorPrefix"] = "Status: ",
                    ["Error.Initialize"] = "Nie udało się zainicjalizować ReportDesigner.",
                    ["Error.SwitchDesign"] = "Nie udało się przełączyć do trybu projektowania.",
                    ["Error.SwitchPreview"] = "Nie udało się przełączyć do trybu podglądu.",
                    ["Error.RefreshPreview"] = "Nie udało się odświeżyć podglądu raportu.",
                    ["Error.Print"] = "Nie udało się uruchomić wydruku.",
                }
            };

        public static string NormalizeLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                return "en";
            }

            string normalized = locale.Trim();
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

        public static string GetText(string locale, string key)
        {
            string normalizedLocale = NormalizeLocale(locale);
            if (Languages.TryGetValue(normalizedLocale, out var language) &&
                language.TryGetValue(key, out var localized))
            {
                return localized;
            }

            return Languages["en"].TryGetValue(key, out var fallback)
                ? fallback
                : key;
        }

        public static string Format(string locale, string key, params object[] arguments)
        {
            return string.Format(GetText(locale, key), arguments ?? Array.Empty<object>());
        }
    }
}
