using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PhialeTech.Components.Shared.Localization;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoFeatureCatalog
    {
        private static readonly IReadOnlyList<DemoLanguageOption> LanguageOptions = new[]
        {
            new DemoLanguageOption("en", "EN"),
            new DemoLanguageOption("pl", "PL"),
        };

        private static readonly IReadOnlyList<DemoExampleDefinition> ExampleDefinitions = new[]
        {
            new DemoExampleDefinition("foundations", "grid", DemoTextKeys.SectionOverview, 0, -1, DemoTextKeys.ExampleFoundationsTitle, DemoTextKeys.ExampleFoundationsDescription, "#0B79D0", new[] { "tokens", "foundations", "design system", "typography", "surfaces" }, drawerGroupId: "foundations"),
            new DemoExampleDefinition("application-state-manager", "application-state-manager", DemoTextKeys.SectionOverview, 0, 0, DemoTextKeys.ExampleApplicationStateManagerTitle, DemoTextKeys.ExampleApplicationStateManagerDescription, "#1D4ED8", new[] { "application state", "state key", "overlay", "persist", "restore" }, drawerGroupId: "architecture"),
            new DemoExampleDefinition("definition-manager", "definition-manager", DemoTextKeys.SectionOverview, 0, 1, DemoTextKeys.ExampleDefinitionManagerTitle, DemoTextKeys.ExampleDefinitionManagerDescription, "#2563EB", new[] { "definitions", "yaml", "dynamic ui", "source", "resolution" }, drawerGroupId: "architecture"),
            new DemoExampleDefinition("grouping", "grid", DemoTextKeys.SectionOverview, 0, 0, DemoTextKeys.ExampleGroupingTitle, DemoTextKeys.ExampleGroupingDescription, "#0D9488", new[] { "grouping", "category", "geometry", "gis" }),
            new DemoExampleDefinition("filtering", "grid", DemoTextKeys.SectionOverview, 0, 1, DemoTextKeys.ExampleFilteringTitle, DemoTextKeys.ExampleFilteringDescription, "#EA580C", new[] { "filter", "municipality", "status", "owner" }),
            new DemoExampleDefinition("sorting", "grid", DemoTextKeys.SectionOverview, 0, 2, DemoTextKeys.ExampleSortingTitle, DemoTextKeys.ExampleSortingDescription, "#2563EB", new[] { "sorting", "inspection", "area", "length" }),
            new DemoExampleDefinition("remote-data", "grid", DemoTextKeys.SectionOverview, 0, 3, DemoTextKeys.ExampleRemoteTitle, DemoTextKeys.ExampleRemoteDescription, "#059669", new[] { "remote", "paging", "async", "gis" }),
            new DemoExampleDefinition("hierarchy", "grid", DemoTextKeys.SectionOverview, 0, 4, DemoTextKeys.ExampleHierarchyTitle, DemoTextKeys.ExampleHierarchyDescription, "#7C3AED", new[] { "hierarchy", "tree", "lazy", "detail" }),
            new DemoExampleDefinition("master-detail", "grid", DemoTextKeys.SectionOverview, 0, 5, DemoTextKeys.ExampleMasterDetailTitle, DemoTextKeys.ExampleMasterDetailDescription, "#0F766E", new[] { "master-detail", "detail", "category", "records" }),
            new DemoExampleDefinition("active-layer-selector", "active-layer-selector", DemoTextKeys.SectionOverview, 0, 6, DemoTextKeys.ExampleActiveLayerSelectorTitle, DemoTextKeys.ExampleActiveLayerSelectorDescription, "#2F80ED", new[] { "layers", "selector", "active", "visibility", "snapping" }),
            new DemoExampleDefinition("yaml-inputs", "yaml-ui", DemoTextKeys.SectionInputs, 4, 0, DemoTextKeys.ExampleYamlInputsTitle, DemoTextKeys.ExampleYamlInputsDescription, "#7C3AED", new[] { "yaml", "input", "textbox", "wpf", "framed", "inline" }, drawerGroupId: "yaml-ui"),
            new DemoExampleDefinition("yaml-primitives", "yaml-ui", DemoTextKeys.SectionInputs, 4, 1, DemoTextKeys.ExampleYamlPrimitivesTitle, DemoTextKeys.ExampleYamlPrimitivesDescription, "#7137E8", new[] { "yaml", "primitives", "badge", "button", "skia", "universalinput" }, drawerGroupId: "yaml-ui"),
            new DemoExampleDefinition("yaml-actions", "yaml-ui", DemoTextKeys.SectionInputs, 4, 2, DemoTextKeys.ExampleYamlActionsTitle, DemoTextKeys.ExampleYamlActionsDescription, "#6D28D9", new[] { "yaml", "actions", "buttons", "strip", "wpf", "document" }, drawerGroupId: "yaml-ui"),
            new DemoExampleDefinition("yaml-document", "yaml-ui", DemoTextKeys.SectionInputs, 4, 3, DemoTextKeys.ExampleYamlDocumentTitle, DemoTextKeys.ExampleYamlDocumentDescription, "#5B21B6", new[] { "yaml", "document", "layout", "actions", "wpf", "renderer", "extends" }, drawerGroupId: "yaml-ui"),
            new DemoExampleDefinition("yaml-advanced-controls", "yaml-ui", DemoTextKeys.SectionInputs, 4, 4, DemoTextKeys.ExampleYamlAdvancedControlsTitle, DemoTextKeys.ExampleYamlAdvancedControlsDescription, "#4C1D95", new[] { "yaml", "documenteditor", "tiptap", "inline", "framed", "advanced" }, drawerGroupId: "yaml-ui"),
            new DemoExampleDefinition("web-host", "web-components", DemoTextKeys.SectionOverview, 0, 7, DemoTextKeys.ExampleWebHostTitle, DemoTextKeys.ExampleWebHostDescription, "#0EA5E9", new[] { "web", "host", "browser", "cef", "webview2", "javascript" }, drawerGroupId: "web-components"),
            new DemoExampleDefinition("pdf-viewer", "web-components", DemoTextKeys.SectionOverview, 0, 8, DemoTextKeys.ExamplePdfViewerTitle, DemoTextKeys.ExamplePdfViewerDescription, "#2563EB", new[] { "pdf", "viewer", "print", "pdfjs", "webhost", "search" }, drawerGroupId: "web-components"),
            new DemoExampleDefinition("report-designer", "web-components", DemoTextKeys.SectionOverview, 0, 9, DemoTextKeys.ExampleReportDesignerTitle, DemoTextKeys.ExampleReportDesignerDescription, "#0F766E", new[] { "report", "designer", "print", "preview", "json", "webhost" }, drawerGroupId: "web-components"),
            new DemoExampleDefinition("monaco-editor", "web-components", DemoTextKeys.SectionOverview, 0, 10, DemoTextKeys.ExampleMonacoEditorTitle, DemoTextKeys.ExampleMonacoEditorDescription, "#1D4ED8", new[] { "monaco", "editor", "code", "yaml", "webhost", "javascript" }, drawerGroupId: "web-components"),
            new DemoExampleDefinition("document-editor", "web-components", DemoTextKeys.SectionOverview, 0, 11, DemoTextKeys.ExampleDocumentEditorTitle, DemoTextKeys.ExampleDocumentEditorDescription, "#0B8F6A", new[] { "documenteditor", "tiptap", "markdown", "html", "json", "webhost" }, drawerGroupId: "web-components"),
            new DemoExampleDefinition("web-component-scroll-host", "web-components", DemoTextKeys.SectionOverview, 0, 12, DemoTextKeys.ExampleWebComponentScrollHostTitle, DemoTextKeys.ExampleWebComponentScrollHostDescription, "#0F766E", new[] { "webview2", "composition", "scroll", "viewport", "focus", "host" }, drawerGroupId: "web-components"),
            new DemoExampleDefinition("my-license", "license", DemoTextKeys.SectionOverview, 0, 11, DemoTextKeys.ExampleMyLicenseTitle, DemoTextKeys.ExampleMyLicenseDescription, "#475569", new[] { "license", "product", "placeholder" }, drawerGroupId: "license"),
            new DemoExampleDefinition("third-party-licenses", "license", DemoTextKeys.SectionOverview, 0, 13, DemoTextKeys.ExampleThirdPartyLicensesTitle, DemoTextKeys.ExampleThirdPartyLicensesDescription, "#0F766E", new[] { "licenses", "third-party", "pdfjs", "jsbarcode", "qrcode", "monaco", "documenteditor" }, drawerGroupId: "license"),
            new DemoExampleDefinition("column-layout", "grid", DemoTextKeys.SectionCustomization, 1, 0, DemoTextKeys.ExampleColumnLayoutTitle, DemoTextKeys.ExampleColumnLayoutDescription, "#7C3AED", new[] { "layout", "column", "owner", "freeze" }),
            new DemoExampleDefinition("state-persistence", "grid", DemoTextKeys.SectionCustomization, 1, 1, DemoTextKeys.ExampleStateTitle, DemoTextKeys.ExampleStateDescription, "#4338CA", new[] { "state", "save", "restore", "gis" }),
            new DemoExampleDefinition("personalization", "grid", DemoTextKeys.SectionCustomization, 1, 2, DemoTextKeys.ExamplePersonalizationTitle, DemoTextKeys.ExamplePersonalizationDescription, "#B45309", new[] { "personalization", "views", "chooser", "search" }),
            new DemoExampleDefinition("export-import", "grid", DemoTextKeys.SectionCustomization, 1, 3, DemoTextKeys.ExampleExportImportTitle, DemoTextKeys.ExampleExportImportDescription, "#0F766E", new[] { "export", "import", "csv", "exchange" }),
            new DemoExampleDefinition("selection", "grid", DemoTextKeys.SectionEditing, 2, 0, DemoTextKeys.ExampleSelectionTitle, DemoTextKeys.ExampleSelectionDescription, "#DC2626", new[] { "selection", "clipboard", "cells", "features" }),
            new DemoExampleDefinition("editing", "grid", DemoTextKeys.SectionEditing, 2, 1, DemoTextKeys.ExampleEditingTitle, DemoTextKeys.ExampleEditingDescription, "#B45309", new[] { "editing", "owner", "status", "priority" }),
            new DemoExampleDefinition("constraints", "grid", DemoTextKeys.SectionEditing, 2, 2, DemoTextKeys.ExampleConstraintsTitle, DemoTextKeys.ExampleConstraintsDescription, "#DC2626", new[] { "constraints", "validation", "text", "decimal", "date", "lookup" }),
            new DemoExampleDefinition("rich-editors", "grid", DemoTextKeys.SectionEditing, 2, 3, DemoTextKeys.ExampleRichEditorsTitle, DemoTextKeys.ExampleRichEditorsDescription, "#2563EB", new[] { "combo", "datepicker", "autocomplete", "masked" }),
            new DemoExampleDefinition("summaries", "grid", DemoTextKeys.SectionAggregates, 3, 0, DemoTextKeys.ExampleSummariesTitle, DemoTextKeys.ExampleSummariesDescription, "#0F766E", new[] { "summary", "area", "length", "count" }),
            new DemoExampleDefinition("summary-designer", "grid", DemoTextKeys.SectionAggregates, 3, 1, DemoTextKeys.ExampleSummaryDesignerTitle, DemoTextKeys.ExampleSummaryDesignerDescription, "#1D4ED8", new[] { "summary designer", "aggregate", "summary", "footer" }),
        };

        private static readonly IReadOnlyList<(string Id, string TitleKey, string DescriptionKey, string AccentHex)> DrawerGroupDefinitions = new[]
        {
            ("license", DemoTextKeys.ShellComponentLicense, DemoTextKeys.ShellComponentLicenseDescription, "#475569"),
            ("foundations", DemoTextKeys.ShellComponentFoundations, DemoTextKeys.ShellComponentFoundationsDescription, "#0B79D0"),
            ("architecture", DemoTextKeys.ShellComponentArchitecture, DemoTextKeys.ShellComponentArchitectureDescription, "#1D4ED8"),
            ("grid", DemoTextKeys.ShellComponentGrid, DemoTextKeys.ShellComponentGridDescription, "#0D9488"),
            ("active-layer-selector", DemoTextKeys.ShellComponentActiveLayerSelector, DemoTextKeys.ShellComponentActiveLayerSelectorDescription, "#2F80ED"),
            ("yaml-ui", DemoTextKeys.ShellComponentYamlUi, DemoTextKeys.ShellComponentYamlUiDescription, "#7C3AED"),
            ("web-components", DemoTextKeys.ShellComponentWebComponents, DemoTextKeys.ShellComponentWebComponentsDescription, "#0EA5E9"),
        };

        private static readonly (string Key, string AccentHex, Func<IReadOnlyList<DemoGisRecordViewModel>, int> ValueSelector)[] MetricDefinitions =
        {
            (DemoTextKeys.MetricTotalFeatures, "#0B79D0", records => records.Count),
            (DemoTextKeys.MetricVisibleFeatures, "#1173C6", records => records.Count(record => record.Visible)),
            (DemoTextKeys.MetricEditableFeatures, "#1D7EE3", records => records.Count(record => record.EditableFlag)),
            (DemoTextKeys.MetricCriticalPriority, "#155BA1", records => records.Count(record => string.Equals(record.Priority, "Critical", StringComparison.OrdinalIgnoreCase))),
        };

        private static readonly IReadOnlyList<DemoGisRecordViewModel> GisRecords = DemoGisDataLoader.LoadDefaultRecords();

        private readonly DemoLocalizationCatalog _localizationCatalog;

        public DemoFeatureCatalog(DemoLocalizationCatalog localizationCatalog = null)
        {
            _localizationCatalog = localizationCatalog ?? LoadDefaultCatalog();
        }

        public IReadOnlyList<DemoLanguageOption> GetLanguageOptions()
        {
            return LanguageOptions;
        }

        public IReadOnlyList<DemoExampleDefinition> GetExamples()
        {
            return ExampleDefinitions;
        }

        public DemoExampleDefinition GetExampleById(string exampleId)
        {
            return ExampleDefinitions.FirstOrDefault(example => string.Equals(example.Id, exampleId, StringComparison.OrdinalIgnoreCase));
        }

        public DemoExampleDefinition GetDefaultExampleByComponentId(string componentId)
        {
            if (string.IsNullOrWhiteSpace(componentId))
            {
                return null;
            }

            if (string.Equals(componentId, "grid", StringComparison.OrdinalIgnoreCase))
            {
                return GetExampleById("grouping");
            }

            if (string.Equals(componentId, "license", StringComparison.OrdinalIgnoreCase))
            {
                return GetExampleById("my-license");
            }

            return ExampleDefinitions
                .Where(example => string.Equals(example.ComponentId, componentId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(example => example.SectionOrder)
                .ThenBy(example => example.DisplayOrder)
                .FirstOrDefault();
        }

        public DemoExampleDefinition GetDefaultExampleByDrawerGroupId(string drawerGroupId)
        {
            return GetExamplesByDrawerGroupId(drawerGroupId).FirstOrDefault();
        }

        public IReadOnlyList<DemoDrawerGroupViewModel> BuildDrawerGroups(string languageCode, string selectedDrawerGroupId)
        {
            return DrawerGroupDefinitions
                .Select(group => new DemoDrawerGroupViewModel(
                    group.Id,
                    Localize(languageCode, group.TitleKey),
                    Localize(languageCode, group.DescriptionKey),
                    group.AccentHex,
                    string.Equals(group.Id, selectedDrawerGroupId, StringComparison.OrdinalIgnoreCase),
                    GetExamplesByDrawerGroupId(group.Id).Count <= 1))
                .ToArray();
        }

        public IReadOnlyList<DemoSectionViewModel> BuildSections(string languageCode, string searchText, string componentFilter = null)
        {
            return BuildSectionsCore(languageCode, searchText, example => MatchesComponent(example, componentFilter));
        }

        public IReadOnlyList<DemoSectionViewModel> BuildSectionsForDrawerGroup(string languageCode, string searchText, string drawerGroupId)
        {
            return BuildSectionsCore(languageCode, searchText, example => MatchesDrawerGroup(example, drawerGroupId));
        }

        public IReadOnlyList<DemoExampleDefinition> GetExamplesByDrawerGroupId(string drawerGroupId)
        {
            var normalizedDrawerGroupId = NormalizeSearch(drawerGroupId);
            if (normalizedDrawerGroupId.Length == 0)
            {
                return Array.Empty<DemoExampleDefinition>();
            }

            return ExampleDefinitions
                .Where(example => MatchesDrawerGroup(example, normalizedDrawerGroupId))
                .OrderBy(example => example.SectionOrder)
                .ThenBy(example => example.DisplayOrder)
                .ToArray();
        }

        public IReadOnlyList<DemoMetricCardViewModel> BuildMetricCards(string languageCode)
        {
            var culture = ResolveCulture(languageCode);
            return MetricDefinitions
                .Select(metric => new DemoMetricCardViewModel(
                    Localize(languageCode, metric.Key),
                    metric.AccentHex,
                    metric.ValueSelector(GisRecords).ToString("N0", culture),
                    true))
                .ToArray();
        }

        public IReadOnlyList<DemoGisPreviewRowViewModel> BuildPreviewRows(string languageCode)
        {
            var culture = ResolveCulture(languageCode);
            return GisRecords
                .Take(12)
                .Select(record => new DemoGisPreviewRowViewModel(
                    record.Category,
                    record.ObjectName,
                    record.Municipality,
                    record.Status,
                    record.AreaSquareMeters.ToString("N2", culture) + " m2",
                    record.LastInspection.ToString("yyyy-MM-dd", culture),
                    ResolveStatusForeground(record.Priority),
                    ResolveInspectionForeground(record.LastInspection)))
                .ToArray();
        }

        public IReadOnlyList<DemoGisRecordViewModel> GetGisRecords()
        {
            return GisRecords.Select(record => (DemoGisRecordViewModel)record.Clone()).ToArray();
        }

        public IReadOnlyList<DemoCodeFileViewModel> GetCodeFiles(string platformKey, string exampleId)
        {
            var generated = DemoCodeSnippetBuilder.Build(platformKey, exampleId);
            if (generated.Count > 0)
            {
                return generated;
            }

            var prefix = $"{typeof(DemoFeatureCatalog).Assembly.GetName().Name}.CodeSamples.{NormalizePlatformKey(platformKey)}.";
            return typeof(DemoFeatureCatalog).Assembly
                .GetManifestResourceNames()
                .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new DemoCodeFileViewModel(GetFileNameFromResourceName(prefix, name), ReadEmbeddedResource(name)))
                .ToArray();
        }

        public string Localize(string languageCode, string key)
        {
            return _localizationCatalog.GetText(languageCode, key);
        }

        private static DemoLocalizationCatalog LoadDefaultCatalog()
        {
            try
            {
                return DemoLocalizationCatalog.LoadDefault();
            }
            catch
            {
                return DemoLocalizationCatalog.Empty;
            }
        }

        private static string NormalizeSearch(string searchText)
        {
            return string.IsNullOrWhiteSpace(searchText) ? string.Empty : searchText.Trim();
        }

        private bool MatchesSearch(DemoExampleDefinition example, string languageCode, string normalizedSearch)
        {
            if (normalizedSearch.Length == 0)
            {
                return true;
            }

            var haystack = new StringBuilder();
            haystack.Append(Localize(languageCode, example.TitleKey));
            haystack.Append(' ');
            haystack.Append(Localize(languageCode, example.DescriptionKey));
            haystack.Append(' ');
            haystack.Append(string.Join(" ", example.Tags));

            return haystack.ToString().IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private IReadOnlyList<DemoSectionViewModel> BuildSectionsCore(string languageCode, string searchText, Func<DemoExampleDefinition, bool> filter)
        {
            var normalizedSearch = NormalizeSearch(searchText);
            return ExampleDefinitions
                .Where(example => filter(example))
                .Where(example => MatchesSearch(example, languageCode, normalizedSearch))
                .GroupBy(example => new { example.SectionKey, example.SectionOrder })
                .OrderBy(group => group.Key.SectionOrder)
                .Select(group => new DemoSectionViewModel(
                    group.Key.SectionKey,
                    Localize(languageCode, group.Key.SectionKey),
                    group.OrderBy(example => example.DisplayOrder)
                        .Select(example => new DemoExampleCardViewModel(
                            example.Id,
                            Localize(languageCode, example.TitleKey),
                            Localize(languageCode, example.DescriptionKey),
                            example.AccentHex))))
                .ToArray();
        }

        private static bool MatchesComponent(DemoExampleDefinition example, string componentFilter)
        {
            var normalizedComponentFilter = NormalizeSearch(componentFilter);
            if (normalizedComponentFilter.Length == 0)
            {
                return true;
            }

            return string.Equals(example.ComponentId, normalizedComponentFilter, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesDrawerGroup(DemoExampleDefinition example, string drawerGroupId)
        {
            var normalizedDrawerGroupId = NormalizeSearch(drawerGroupId);
            if (normalizedDrawerGroupId.Length == 0)
            {
                return true;
            }

            return string.Equals(example.DrawerGroupId, normalizedDrawerGroupId, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePlatformKey(string platformKey)
        {
            if (string.IsNullOrWhiteSpace(platformKey))
            {
                return "Wpf";
            }

            return platformKey.Trim();
        }

        private static string GetFileNameFromResourceName(string prefix, string resourceName)
        {
            return resourceName.Substring(prefix.Length).Replace(".txt", string.Empty);
        }

        private static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = typeof(DemoFeatureCatalog).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new System.IO.StreamReader(stream ?? throw new InvalidOperationException("Resource not found: " + resourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        private static CultureInfo ResolveCulture(string languageCode)
        {
            var normalizedLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
            return normalizedLanguageCode == "pl"
                ? CultureInfo.GetCultureInfo("pl-PL")
                : CultureInfo.GetCultureInfo("en-US");
        }

        private static string ResolveStatusForeground(string priority)
        {
            if (string.Equals(priority, "Critical", StringComparison.OrdinalIgnoreCase))
            {
                return "#C81E1E";
            }

            if (string.Equals(priority, "High", StringComparison.OrdinalIgnoreCase))
            {
                return "#B45309";
            }

            return "#1F4F7A";
        }

        private static string ResolveInspectionForeground(DateTime lastInspection)
        {
            return lastInspection >= new DateTime(2025, 10, 1)
                ? "#166534"
                : "#52606D";
        }
    }
}
