using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PhialeTech.Components.Shared.Model;

namespace PhialeTech.Components.Shared.Services
{
    internal static class DemoCodeSnippetBuilder
    {
        private static readonly ColumnSpec[] Columns =
        {
            new ColumnSpec("Category", "Category", "string", 150d, 0, false),
            new ColumnSpec("ObjectName", "Object name", "string", 260d, 1, true),
            new ColumnSpec("ObjectId", "Object ID", "string", 180d, 2, false),
            new ColumnSpec("GeometryType", "Geometry type", "string", 130d, 3, false),
            new ColumnSpec("Municipality", "Municipality", "string", 130d, 4, false),
            new ColumnSpec("District", "District", "string", 140d, 5, false),
            new ColumnSpec("Status", "Status", "string", 150d, 6, true, editorKind: "GridColumnEditorKind.Combo", editorItems: "new[] { \"Active\", \"Verified\", \"NeedsReview\", \"UnderMaintenance\", \"Planned\", \"Retired\" }"),
            new ColumnSpec("Priority", "Priority", "string", 120d, 7, true, editorKind: "GridColumnEditorKind.Combo", editorItems: "new[] { \"Critical\", \"High\", \"Medium\", \"Low\" }"),
            new ColumnSpec("AreaSquareMeters", "Area [m2]", "decimal", 140d, 8, false),
            new ColumnSpec("LengthMeters", "Length [m]", "decimal", 140d, 9, false),
            new ColumnSpec("LastInspection", "Last inspection", "DateTime", 150d, 10, true, editorKind: "GridColumnEditorKind.DatePicker"),
            new ColumnSpec("Owner", "Owner", "string", 180d, 11, true, editorKind: "GridColumnEditorKind.Autocomplete", editorItems: "new[] { \"City Infrastructure\", \"Field Team Alpha\", \"Field Team Beta\", \"Municipal Dispatch\", \"Utilities North\" }"),
            new ColumnSpec("ScaleHint", "Scale hint", "int", 120d, 12, true, editorKind: "GridColumnEditorKind.MaskedText", editMask: "\"^[0-9]{0,6}$\""),
        };

        private static readonly ColumnSpec[] MasterDetailColumns =
        {
            new ColumnSpec("Category", "Category", "string", 170d, 0, false),
            new ColumnSpec("Description", "Description", "string", 360d, 1, false),
            new ColumnSpec("ObjectName", "Object name", "string", 260d, 2, false),
            new ColumnSpec("ObjectId", "Object ID", "string", 180d, 3, false),
            new ColumnSpec("GeometryType", "Geometry type", "string", 140d, 4, false),
            new ColumnSpec("Status", "Status", "string", 150d, 5, false),
        };

        public static IReadOnlyList<DemoCodeFileViewModel> Build(string platformKey, string exampleId)
        {

            if (string.Equals(exampleId, "active-layer-selector", StringComparison.OrdinalIgnoreCase))
            {
                return BuildActiveLayerSelectorFiles(platformKey);
            }

            if (string.Equals(exampleId, "foundations", StringComparison.OrdinalIgnoreCase))
            {
                return BuildFoundationsFiles(platformKey);
            }

            if (string.Equals(exampleId, "application-state-manager", StringComparison.OrdinalIgnoreCase))
            {
                return BuildApplicationStateManagerFiles(platformKey);
            }

            if (string.Equals(exampleId, "definition-manager", StringComparison.OrdinalIgnoreCase))
            {
                return BuildDefinitionManagerFiles(platformKey);
            }

            if (string.Equals(exampleId, "web-host", StringComparison.OrdinalIgnoreCase))
            {
                return BuildWebHostFiles(platformKey);
            }

            if (string.Equals(exampleId, "pdf-viewer", StringComparison.OrdinalIgnoreCase))
            {
                return BuildPdfViewerFiles(platformKey);
            }

            if (string.Equals(exampleId, "report-designer", StringComparison.OrdinalIgnoreCase))
            {
                return BuildReportDesignerFiles(platformKey);
            }

            if (string.Equals(exampleId, "monaco-editor", StringComparison.OrdinalIgnoreCase))
            {
                return BuildMonacoEditorFiles(platformKey);
            }

            if (string.Equals(exampleId, "yaml-document", StringComparison.OrdinalIgnoreCase))
            {
                return BuildYamlDocumentFiles(platformKey);
            }

            if (string.Equals(exampleId, "yaml-actions", StringComparison.OrdinalIgnoreCase))
            {
                return BuildYamlActionsFiles(platformKey);
            }

            if (string.Equals(exampleId, "yaml-generate-form", StringComparison.OrdinalIgnoreCase))
            {
                return BuildYamlGenerateFormFiles(platformKey);
            }

            if (string.Equals(exampleId, "my-license", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new DemoCodeFileViewModel("MY_LICENSE.md", DemoLicenseCatalog.BuildMyLicenseMarkdown())
                };
            }

            if (string.Equals(exampleId, "third-party-licenses", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    new DemoCodeFileViewModel("THIRD_PARTY_LICENSES.md", DemoLicenseCatalog.BuildThirdPartyMarkdown())
                };
            }

            var spec = PlatformSpec.Create(platformKey);
            var normalizedExampleId = string.IsNullOrWhiteSpace(exampleId) ? "grouping" : exampleId.Trim();
            var files = new List<DemoCodeFileViewModel>
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildMarkup(spec, normalizedExampleId)),
                new DemoCodeFileViewModel("ExampleViewModel.cs", BuildViewModel(normalizedExampleId)),
            };

            var hostCode = BuildHostFile(spec, normalizedExampleId);
            if (!string.IsNullOrWhiteSpace(hostCode))
            {
                files.Add(new DemoCodeFileViewModel(spec.HostCodeFileName, hostCode));
            }

            if (string.Equals(platformKey, "Wpf", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(normalizedExampleId, "state-persistence", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new DemoCodeFileViewModel("App.xaml.cs", BuildApplicationStateAppHostFile()));
            }

            return files;
        }

        private static string BuildHostFile(PlatformSpec spec, string exampleId)
        {
            var hostMembers = BuildHostCode(exampleId);
            if (!spec.WrapHostInClass)
            {
                return hostMembers;
            }

            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Windows;");
            builder.AppendLine("using System.Windows.Controls;");
            builder.AppendLine("using PhialeTech.ComponentHost.State;");
            builder.AppendLine("using PhialeGrid.Core.State;");
            builder.AppendLine("using PhialeTech.Components.Shared.Services;");
            builder.AppendLine("using PhialeTech.Components.Shared.ViewModels;");
            builder.AppendLine("using PhialeTech.PhialeGrid.Wpf.Controls;");
            builder.AppendLine();
            builder.AppendLine("namespace Demo.Snippets");
            builder.AppendLine("{");
            builder.AppendLine("    public sealed partial class ExampleHost : " + spec.HostBaseType);
            builder.AppendLine("    {");
            builder.AppendLine("        public " + GetClassName(exampleId) + " ViewModel { get; } = new " + GetClassName(exampleId) + "();");
            builder.AppendLine();
            builder.AppendLine("        public ExampleHost()");
            builder.AppendLine("        {");
            builder.AppendLine("            InitializeComponent();");
            builder.AppendLine("            DataContext = ViewModel;");
            if (exampleId == "hierarchy")
            {
                builder.AppendLine("            ConfigureHierarchy();");
            }
            else if (exampleId == "master-detail")
            {
                builder.AppendLine("            ConfigureMasterDetail();");
            }
            else if (exampleId == "state-persistence")
            {
                builder.AppendLine("            ConfigureStatePersistence();");
            }

            builder.AppendLine("        }");

            if (!string.IsNullOrWhiteSpace(hostMembers))
            {
                builder.AppendLine();
                AppendIndentedBlock(builder, hostMembers, 8);
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString().TrimEnd();
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildActiveLayerSelectorFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildActiveLayerSelectorMarkup(platformKey)),
                new DemoCodeFileViewModel("ExampleViewModel.cs", BuildActiveLayerSelectorViewModel()),
                new DemoCodeFileViewModel("ExampleState.cs", BuildActiveLayerSelectorState()),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildFoundationsFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            var files = new List<DemoCodeFileViewModel>
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildFoundationsMarkup(spec)),
                new DemoCodeFileViewModel("ExampleViewModel.cs", BuildFoundationsViewModel()),
            };

            var hostCode = BuildFoundationsHostFile(spec);
            if (!string.IsNullOrWhiteSpace(hostCode))
            {
                files.Add(new DemoCodeFileViewModel(spec.HostCodeFileName, hostCode));
            }

            return files;
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildApplicationStateManagerFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            var files = new List<DemoCodeFileViewModel>
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildMarkup(spec, "state-persistence")),
                new DemoCodeFileViewModel("ExampleViewModel.cs", BuildViewModel("state-persistence")),
            };

            var hostCode = BuildHostFile(spec, "state-persistence");
            if (!string.IsNullOrWhiteSpace(hostCode))
            {
                files.Add(new DemoCodeFileViewModel(spec.HostCodeFileName, hostCode));
            }

            if (string.Equals(platformKey, "Wpf", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new DemoCodeFileViewModel("App.xaml.cs", BuildApplicationStateAppHostFile()));
            }

            return files;
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildDefinitionManagerFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            var files = new List<DemoCodeFileViewModel>
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildDefinitionManagerMarkup(spec)),
                new DemoCodeFileViewModel("ExampleViewModel.cs", BuildDefinitionManagerViewModel()),
                new DemoCodeFileViewModel("ExampleDefinition.cs", BuildDefinitionManagerDefinitionFile()),
            };

            if (string.Equals(platformKey, "Wpf", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(new DemoCodeFileViewModel("App.xaml.cs", BuildDefinitionManagerAppHostFile()));
            }

            return files;
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildWebHostFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildWebHostMarkup(platformKey)),
                new DemoCodeFileViewModel(spec.HostCodeFileName, BuildWebHostHostFile(platformKey)),
                new DemoCodeFileViewModel("web-host-entry.html", BuildWebHostHtmlFile()),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildPdfViewerFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildPdfViewerMarkup(platformKey)),
                new DemoCodeFileViewModel(spec.HostCodeFileName, BuildPdfViewerHostFile(platformKey)),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildReportDesignerFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildReportDesignerMarkup(platformKey)),
                new DemoCodeFileViewModel(spec.HostCodeFileName, BuildReportDesignerHostFile(platformKey)),
                new DemoCodeFileViewModel("ReportDesignerSample.cs", BuildReportDesignerSampleFile()),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildMonacoEditorFiles(string platformKey)
        {
            if (!string.Equals(platformKey, "Wpf", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<DemoCodeFileViewModel>();
            }

            return new[]
            {
                new DemoCodeFileViewModel("Example.xaml", BuildMonacoEditorMarkup()),
                new DemoCodeFileViewModel("MonacoEditorHost.cs", BuildMonacoEditorHostFile()),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildYamlDocumentFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildYamlDocumentMarkup(platformKey)),
                new DemoCodeFileViewModel(spec.HostCodeFileName, BuildYamlDocumentHostFile(platformKey)),
                new DemoCodeFileViewModel("document.yaml", BuildYamlDocumentYamlSample()),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildYamlActionsFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildYamlDocumentMarkup(platformKey)),
                new DemoCodeFileViewModel(spec.HostCodeFileName, BuildYamlDocumentHostFile(platformKey)),
                new DemoCodeFileViewModel("actions.yaml", BuildYamlActionsYamlSample()),
            };
        }

        private static IReadOnlyList<DemoCodeFileViewModel> BuildYamlGenerateFormFiles(string platformKey)
        {
            var spec = PlatformSpec.Create(platformKey);
            return new[]
            {
                new DemoCodeFileViewModel("Example" + spec.XamlExtension, BuildYamlDocumentMarkup(platformKey)),
                new DemoCodeFileViewModel(spec.HostCodeFileName, BuildYamlDocumentHostFile(platformKey)),
                new DemoCodeFileViewModel("generate-form.yaml", BuildYamlGenerateFormYamlSample()),
            };
        }

        private static string BuildYamlDocumentMarkup(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl xmlns=\"https://github.com/avaloniaui\"",
                    "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "             x:Class=\"Demo.Snippets.ExampleHost\">",
                    "  <Grid RowDefinitions=\"Auto,*\">",
                    "    <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "               FontSize=\"20\"",
                    "               Text=\"Yaml document host\" />",
                    "    <ContentControl x:Name=\"DocumentPresenter\" Grid.Row=\"1\" Margin=\"0,16,0,0\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl",
                    "    x:Class=\"Demo.Snippets.ExampleHost\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Grid>",
                    "    <Grid.RowDefinitions>",
                    "      <RowDefinition Height=\"Auto\" />",
                    "      <RowDefinition Height=\"*\" />",
                    "    </Grid.RowDefinitions>",
                    "    <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "               FontSize=\"20\"",
                    "               Text=\"Yaml document host\" />",
                    "    <ContentControl x:Name=\"DocumentPresenter\" Grid.Row=\"1\" Margin=\"0,16,0,0\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "<UserControl",
                "    x:Class=\"Demo.Snippets.ExampleHost\"",
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                "    xmlns:yamlDocument=\"clr-namespace:PhialeTech.YamlApp.Wpf.Document;assembly=PhialeTech.YamlApp.Wpf\">",
                "  <Grid>",
                "    <Grid.RowDefinitions>",
                "      <RowDefinition Height=\"Auto\" />",
                "      <RowDefinition Height=\"*\" />",
                "    </Grid.RowDefinitions>",
                "    <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                "               FontSize=\"20\"",
                "               Text=\"Yaml document host\" />",
                "    <yamlDocument:YamlDocumentHost x:Name=\"DocumentHost\" Grid.Row=\"1\" Margin=\"0,16,0,0\" />",
                "  </Grid>",
                "</UserControl>",
            });
        }

        private static string BuildYamlDocumentHostFile(string platformKey)
        {
            if (!string.Equals(platformKey, "Wpf", StringComparison.OrdinalIgnoreCase))
            {
                return "// Platform renderer pending. The semantic document runtime is already portable.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                "using System.Windows.Controls;",
                "using PhialeTech.YamlApp.Runtime.Services;",
                "using PhialeTech.YamlApp.Wpf;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public sealed partial class ExampleHost : UserControl",
                "    {",
                "        public ExampleHost()",
                "        {",
                "            InitializeComponent();",
                string.Empty,
                "            var preparation = new DocumentRuntimePreparationService(",
                "                /* configuration source */ null,",
                "                /* importer */ null,",
                "                /* normalizer */ null,",
                "                new RuntimeDocumentStateFactory(),",
                "                new RuntimeDocumentJsonMapper());",
                string.Empty,
                "            var adapter = new YamlAppWpfAdapter();",
                "            // In the real app we pass a prepared RuntimeDocumentState.",
                "            DocumentHost = adapter.CreateDocumentHost(runtimeDocumentState: null);",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildYamlActionsYamlSample()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "namespace: demo.actions",
                "imports:",
                "  - medium",
                "",
                "document:",
                "  id: action-review",
                "  kind: Form",
                "  name: YAML actions demo",
                "  interactionMode: Classic",
                "  densityMode: Normal",
                "  fieldChromeMode: Framed",
                "  actionAreas:",
                "    - id: headerActions",
                "      placement: Top",
                "      horizontalAlignment: Stretch",
                "      shared: true",
                "      sticky: true",
                "    - id: leftTools",
                "      placement: Left",
                "      horizontalAlignment: Stretch",
                "      shared: false",
                "    - id: footerPrimary",
                "      placement: Bottom",
                "      horizontalAlignment: Right",
                "      shared: true",
                "      sticky: true",
                "    - id: rightHelp",
                "      placement: Right",
                "      horizontalAlignment: Stretch",
                "      shared: false",
                "  fields:",
                "    - id: reviewTitle",
                "      extends: limited50Text",
                "      caption: Review title",
                "      placeholder: Action rendering driven by YAML",
                "      widthHint: Medium",
                "    - id: reviewNotes",
                "      extends: notesText",
                "      caption: Notes",
                "      placeholder: Compare area grouping, ordering and primary actions",
                "      widthHint: Fill",
                "  actions:",
                "    - id: help",
                "      semantic: Help",
                "      caption: Help",
                "      area: headerActions",
                "      slot: Start",
                "      order: 10",
                "    - id: docs",
                "      semantic: Secondary",
                "      caption: Documentation",
                "      area: rightHelp",
                "      slot: Start",
                "      order: 10",
                "    - id: history",
                "      semantic: Secondary",
                "      caption: History",
                "      area: leftTools",
                "      slot: Start",
                "      order: 10",
                "    - id: validate",
                "      semantic: Apply",
                "      caption: Validate",
                "      area: headerActions",
                "      slot: End",
                "      order: 20",
                "    - id: save",
                "      semantic: Ok",
                "      caption: Save document",
                "      area: footerPrimary",
                "      isPrimary: true",
                "      slot: End",
                "      order: 10",
                "    - id: cancel",
                "      semantic: Cancel",
                "      caption: Cancel",
                "      area: footerPrimary",
                "      slot: End",
                "      order: 20",
                "  layout:",
                "    type: Column",
                "    items:",
                "      - type: Container",
                "        caption: Action rendering examples",
                "        showBorder: true",
                "        items:",
                "          - fieldRef: reviewTitle",
                "          - fieldRef: reviewNotes",
            });
        }

        private static string BuildYamlGenerateFormYamlSample()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "namespace: application.forms",
                "imports:",
                "  - domain.person",
                "",
                "documents:",
                "  yaml-generated-form:",
                "    kind: Form",
                "    name: YAML generated form",
                "    interactionMode: Classic",
                "    densityMode: Normal",
                "    fieldChromeMode: Framed",
                "    actionAreas:",
                "      - id: footerPrimary",
                "        placement: Bottom",
                "        horizontalAlignment: Right",
                "        shared: true",
                "    fields:",
                "      - id: firstName",
                "        extends: firstName",
                "      - id: lastName",
                "        extends: lastName",
                "      - id: age",
                "        extends: age",
                "      - id: notes",
                "        extends: notes",
                "    layout:",
                "      type: Column",
                "      items:",
                "        - type: Row",
                "          items:",
                "            - fieldRef: firstName",
                "            - fieldRef: lastName",
                "            - fieldRef: age",
                "        - fieldRef: notes",
                "    actions:",
                "      - id: ok",
                "        semantic: Ok",
                "        captionKey: actions.ok.caption",
                "        area: footerPrimary",
                "      - id: cancel",
                "        semantic: Cancel",
                "        captionKey: actions.cancel.caption",
                "        area: footerPrimary",
            });
        }

        private static string BuildYamlDocumentYamlSample()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "id: ops-intake",
                "name: Operational intake document",
                "interactionMode: Touch",
                "densityMode: Comfortable",
                "fieldChromeMode: Framed",
                "fields:",
                "  customerName:",
                "    type: string",
                "    captionKey: Customer",
                "    widthHint: Fill",
                "  ticketCode:",
                "    type: string",
                "    captionKey: Ticket code",
                "    widthHint: Short",
                "  contactEmail:",
                "    type: string",
                "    captionKey: Contact email",
                "    widthHint: Long",
                "layout:",
                "  id: root",
                "  items:",
                "    - type: container",
                "      captionKey: Primary identity",
                "      showBorder: true",
                "      items:",
                "        - type: row",
                "          items:",
                "            - type: fieldRef",
                "              fieldRef: customerName",
                "            - type: fieldRef",
                "              fieldRef: ticketCode",
                "actions:",
                "  - id: ok",
                "    semantic: Ok",
                "    captionKey: Save document",
                "  - id: cancel",
                "    semantic: Cancel",
                "    captionKey: Cancel",
            });
        }

        private static string BuildWebHostMarkup(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl xmlns=\"https://github.com/avaloniaui\"",
                    "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "             x:Class=\"Demo.Snippets.ExampleHost\">",
                    "  <Grid RowDefinitions=\"Auto,*\">",
                    "    <StackPanel Margin=\"0,0,0,12\">",
                    "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "                 FontSize=\"20\"",
                    "                 Text=\"Reusable WebHost\" />",
                    "      <TextBlock Margin=\"0,6,0,0\"",
                    "                 TextWrapping=\"Wrap\"",
                    "                 Text=\"Embed the shared browser host once, then layer a JS protocol or domain control on top.\" />",
                    "    </StackPanel>",
                    "    <ContentControl x:Name=\"HostPresenter\" Grid.Row=\"1\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl",
                    "    x:Class=\"Demo.Snippets.ExampleHost\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Grid>",
                    "    <Grid.RowDefinitions>",
                    "      <RowDefinition Height=\"Auto\" />",
                    "      <RowDefinition Height=\"*\" />",
                    "    </Grid.RowDefinitions>",
                    "    <StackPanel Margin=\"0,0,0,12\">",
                    "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "                 FontSize=\"20\"",
                    "                 Text=\"Reusable WebHost\" />",
                    "      <TextBlock Margin=\"0,6,0,0\"",
                    "                 TextWrapping=\"Wrap\"",
                    "                 Text=\"Embed the shared browser host once, then layer a JS protocol or domain control on top.\" />",
                    "    </StackPanel>",
                    "    <ContentControl x:Name=\"HostPresenter\" Grid.Row=\"1\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "<UserControl",
                "    x:Class=\"Demo.Snippets.ExampleHost\"",
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                "  <Grid>",
                "    <Grid.RowDefinitions>",
                "      <RowDefinition Height=\"Auto\" />",
                "      <RowDefinition Height=\"*\" />",
                "    </Grid.RowDefinitions>",
                "    <StackPanel Margin=\"0,0,0,12\">",
                "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                "                 FontSize=\"20\"",
                "                 Text=\"Reusable WebHost\" />",
                "      <TextBlock Margin=\"0,6,0,0\"",
                "                 TextWrapping=\"Wrap\"",
                "                 Text=\"Embed the shared browser host once, then layer a JS protocol or domain control on top.\" />",
                "    </StackPanel>",
                "    <ContentControl x:Name=\"HostPresenter\" Grid.Row=\"1\" />",
                "  </Grid>",
                "</UserControl>",
            });
        }

        private static string BuildWebHostHostFile(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "using Avalonia.Controls;",
                    "using Avalonia.VisualTree;",
                    "using PhialeTech.WebHost.Abstractions.Ui.Web;",
                    "using PhialeTech.WebHost.Avalonia;",
                    "using System;",
                    "using System.IO;",
                    string.Empty,
                    "namespace Demo.Snippets",
                    "{",
                    "    public sealed partial class ExampleHost : UserControl",
                    "    {",
                    "        private readonly IWebComponentHost _host;",
                    "        private bool _started;",
                    string.Empty,
                    "        public ExampleHost()",
                    "        {",
                    "            InitializeComponent();",
                    string.Empty,
                    "            var factory = new AvaloniaWebComponentHostFactory();",
                    "            _host = factory.CreateHost(new WebComponentHostOptions",
                    "            {",
                    "                LocalContentRootPath = Path.Combine(AppContext.BaseDirectory, \"Assets\"),",
                    "                JavaScriptReadyMessageType = \"demoReady\"",
                    "            });",
                    string.Empty,
                    "            HostPresenter.Content = _host;",
                    "            AttachedToVisualTree += HandleAttachedToVisualTree;",
                    "            DetachedFromVisualTree += HandleDetachedFromVisualTree;",
                    "        }",
                    string.Empty,
                    "        private async void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)",
                    "        {",
                    "            if (_started)",
                    "            {",
                    "                return;",
                    "            }",
                    string.Empty,
                    "            _started = true;",
                    "            await _host.InitializeAsync();",
                    "            await _host.PostMessageAsync(new { type = \"hostGreeting\", platform = \"Avalonia\" });",
                    "            await _host.LoadEntryPageAsync(\"WebHostSample/index.html\");",
                    "        }",
                    string.Empty,
                    "        private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)",
                    "        {",
                    "            _host.Dispose();",
                    "        }",
                    "    }",
                    "}",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "using Microsoft.UI.Xaml;",
                    "using Microsoft.UI.Xaml.Controls;",
                    "using PhialeTech.WebHost.Abstractions.Ui.Web;",
                    "using PhialeTech.WebHost.WinUI;",
                    "using System;",
                    "using System.IO;",
                    string.Empty,
                    "namespace Demo.Snippets",
                    "{",
                    "    public sealed partial class ExampleHost : UserControl",
                    "    {",
                    "        private readonly IWebComponentHost _host;",
                    "        private bool _started;",
                    string.Empty,
                    "        public ExampleHost()",
                    "        {",
                    "            InitializeComponent();",
                    string.Empty,
                    "            var factory = new WinUiWebComponentHostFactory();",
                    "            _host = factory.CreateHost(new WebComponentHostOptions",
                    "            {",
                    "                LocalContentRootPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, \"Assets\"),",
                    "                JavaScriptReadyMessageType = \"demoReady\"",
                    "            });",
                    string.Empty,
                    "            HostPresenter.Content = _host;",
                    "            Loaded += HandleLoaded;",
                    "            Unloaded += HandleUnloaded;",
                    "        }",
                    string.Empty,
                    "        private async void HandleLoaded(object sender, RoutedEventArgs e)",
                    "        {",
                    "            if (_started)",
                    "            {",
                    "                return;",
                    "            }",
                    string.Empty,
                    "            _started = true;",
                    "            await _host.InitializeAsync();",
                    "            await _host.PostMessageAsync(new { type = \"hostGreeting\", platform = \"WinUI3\" });",
                    "            await _host.LoadEntryPageAsync(\"WebHostSample/index.html\");",
                    "        }",
                    string.Empty,
                    "        private void HandleUnloaded(object sender, RoutedEventArgs e)",
                    "        {",
                    "            _host.Dispose();",
                    "        }",
                    "    }",
                    "}",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.WebHost.Abstractions.Ui.Web;",
                "using PhialeTech.WebHost.Wpf;",
                "using System;",
                "using System.IO;",
                "using System.Windows;",
                "using System.Windows.Controls;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public sealed partial class ExampleHost : UserControl",
                "    {",
                "        private readonly IWebComponentHost _host;",
                "        private bool _started;",
                string.Empty,
                "        public ExampleHost()",
                "        {",
                "            InitializeComponent();",
                string.Empty,
                "            var factory = new WpfWebComponentHostFactory();",
                "            _host = factory.CreateHost(new WebComponentHostOptions",
                "            {",
                "                LocalContentRootPath = Path.Combine(AppContext.BaseDirectory, \"Assets\"),",
                "                JavaScriptReadyMessageType = \"demoReady\"",
                "            });",
                string.Empty,
                "            HostPresenter.Content = _host;",
                "            Loaded += HandleLoaded;",
                "            Unloaded += HandleUnloaded;",
                "        }",
                string.Empty,
                "        private async void HandleLoaded(object sender, RoutedEventArgs e)",
                "        {",
                "            if (_started)",
                "            {",
                "                return;",
                "            }",
                string.Empty,
                "            _started = true;",
                "            await _host.InitializeAsync();",
                "            await _host.PostMessageAsync(new { type = \"hostGreeting\", platform = \"WPF\" });",
                "            await _host.LoadEntryPageAsync(\"WebHostSample/index.html\");",
                "        }",
                string.Empty,
                "        private void HandleUnloaded(object sender, RoutedEventArgs e)",
                "        {",
                "            _host.Dispose();",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildPdfViewerMarkup(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl xmlns=\"https://github.com/avaloniaui\"",
                    "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "             x:Class=\"Demo.Snippets.ExampleHost\">",
                    "  <Grid RowDefinitions=\"Auto,*\">",
                    "    <StackPanel Margin=\"0,0,0,12\">",
                    "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "                 FontSize=\"20\"",
                    "                 Text=\"Reusable PdfViewer\" />",
                    "      <TextBlock Margin=\"0,6,0,0\"",
                    "                 TextWrapping=\"Wrap\"",
                    "                 Text=\"Use a native desktop toolbar around one browser-hosted PDF.js surface.\" />",
                    "    </StackPanel>",
                    "    <ContentControl x:Name=\"ViewerPresenter\" Grid.Row=\"1\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl",
                    "    x:Class=\"Demo.Snippets.ExampleHost\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Grid>",
                    "    <Grid.RowDefinitions>",
                    "      <RowDefinition Height=\"Auto\" />",
                    "      <RowDefinition Height=\"*\" />",
                    "    </Grid.RowDefinitions>",
                    "    <StackPanel Margin=\"0,0,0,12\">",
                    "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "                 FontSize=\"20\"",
                    "                 Text=\"Reusable PdfViewer\" />",
                    "      <TextBlock Margin=\"0,6,0,0\"",
                    "                 TextWrapping=\"Wrap\"",
                    "                 Text=\"Use a native desktop toolbar around one browser-hosted PDF.js surface.\" />",
                    "    </StackPanel>",
                    "    <ContentControl x:Name=\"ViewerPresenter\" Grid.Row=\"1\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "<UserControl",
                "    x:Class=\"Demo.Snippets.ExampleHost\"",
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                "  <Grid>",
                "    <Grid.RowDefinitions>",
                "      <RowDefinition Height=\"Auto\" />",
                "      <RowDefinition Height=\"*\" />",
                "    </Grid.RowDefinitions>",
                "    <StackPanel Margin=\"0,0,0,12\">",
                "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                "                 FontSize=\"20\"",
                "                 Text=\"Reusable PdfViewer\" />",
                "      <TextBlock Margin=\"0,6,0,0\"",
                "                 TextWrapping=\"Wrap\"",
                "                 Text=\"Use a native desktop toolbar around one browser-hosted PDF.js surface.\" />",
                "    </StackPanel>",
                "    <ContentControl x:Name=\"ViewerPresenter\" Grid.Row=\"1\" />",
                "  </Grid>",
                "</UserControl>",
            });
        }

        private static string BuildPdfViewerHostFile(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "using Avalonia.Controls;",
                    "using Avalonia.VisualTree;",
                    "using PhialeTech.PdfViewer.Abstractions;",
                    "using PhialeTech.PdfViewer.Avalonia.Controls;",
                    "using PhialeTech.WebHost;",
                    "using System;",
                    string.Empty,
                    "namespace Demo.Snippets",
                    "{",
                    "    public sealed partial class ExampleHost : UserControl",
                    "    {",
                    "        private readonly PhialePdfViewer _viewer;",
                    "        private bool _opened;",
                    string.Empty,
                    "        public ExampleHost()",
                    "        {",
                    "            InitializeComponent();",
                    "            _viewer = new PhialePdfViewer();",
                    "            ViewerPresenter.Content = _viewer;",
                    "            AttachedToVisualTree += HandleAttachedToVisualTree;",
                    "            DetachedFromVisualTree += HandleDetachedFromVisualTree;",
                    "        }",
                    string.Empty,
                    "        private async void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)",
                    "        {",
                    "            if (_opened)",
                    "            {",
                    "                return;",
                    "            }",
                    string.Empty,
                    "            _opened = true;",
                    "            var samplePath = WebAssetLocationResolver.ResolveAssetPath(\"PdfViewer/Samples/phialetech-sample.pdf\");",
                    "            await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(samplePath));",
                    "        }",
                    string.Empty,
                    "        private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)",
                    "        {",
                    "            _viewer.Dispose();",
                    "        }",
                    "    }",
                    "}",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "using Microsoft.UI.Xaml;",
                    "using Microsoft.UI.Xaml.Controls;",
                    "using PhialeTech.PdfViewer.Abstractions;",
                    "using PhialeTech.PdfViewer.WinUI.Controls;",
                    "using PhialeTech.WebHost;",
                    "using System;",
                    string.Empty,
                    "namespace Demo.Snippets",
                    "{",
                    "    public sealed partial class ExampleHost : UserControl",
                    "    {",
                    "        private readonly PhialePdfViewer _viewer;",
                    "        private bool _opened;",
                    string.Empty,
                    "        public ExampleHost()",
                    "        {",
                    "            InitializeComponent();",
                    "            _viewer = new PhialePdfViewer();",
                    "            ViewerPresenter.Content = _viewer;",
                    "            Loaded += HandleLoaded;",
                    "            Unloaded += HandleUnloaded;",
                    "        }",
                    string.Empty,
                    "        private async void HandleLoaded(object sender, RoutedEventArgs e)",
                    "        {",
                    "            if (_opened)",
                    "            {",
                    "                return;",
                    "            }",
                    string.Empty,
                    "            _opened = true;",
                    "            var samplePath = WebAssetLocationResolver.ResolveAssetPath(\"PdfViewer/Samples/phialetech-sample.pdf\");",
                    "            await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(samplePath));",
                    "        }",
                    string.Empty,
                    "        private void HandleUnloaded(object sender, RoutedEventArgs e)",
                    "        {",
                    "            _viewer.Dispose();",
                    "        }",
                    "    }",
                    "}",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.PdfViewer.Abstractions;",
                "using PhialeTech.PdfViewer.Wpf.Controls;",
                "using PhialeTech.WebHost;",
                "using System;",
                "using System.Windows;",
                "using System.Windows.Controls;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public sealed partial class ExampleHost : UserControl",
                "    {",
                "        private readonly PhialePdfViewer _viewer;",
                "        private bool _opened;",
                string.Empty,
                "        public ExampleHost()",
                "        {",
                "            InitializeComponent();",
                "            _viewer = new PhialePdfViewer();",
                "            ViewerPresenter.Content = _viewer;",
                "            Loaded += HandleLoaded;",
                "            Unloaded += HandleUnloaded;",
                "        }",
                string.Empty,
                "        private async void HandleLoaded(object sender, RoutedEventArgs e)",
                "        {",
                "            if (_opened)",
                "            {",
                "                return;",
                "            }",
                string.Empty,
                "            _opened = true;",
                "            var samplePath = WebAssetLocationResolver.ResolveAssetPath(\"PdfViewer/Samples/phialetech-sample.pdf\");",
                "            await _viewer.OpenAsync(PdfDocumentSource.FromFilePath(samplePath));",
                "        }",
                string.Empty,
                "        private void HandleUnloaded(object sender, RoutedEventArgs e)",
                "        {",
                "            _viewer.Dispose();",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildReportDesignerMarkup(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl xmlns=\"https://github.com/avaloniaui\"",
                    "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "             x:Class=\"Demo.Snippets.ExampleHost\">",
                    "  <Grid RowDefinitions=\"Auto,*\">",
                    "    <StackPanel Margin=\"0,0,0,12\">",
                    "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "                 FontSize=\"20\"",
                    "                 Text=\"Reusable ReportDesigner\" />",
                    "      <TextBlock Margin=\"0,6,0,0\"",
                    "                 TextWrapping=\"Wrap\"",
                    "                 Text=\"Keep the shell native and host one browser-based report designer on top of the neutral WebHost.\" />",
                    "    </StackPanel>",
                    "    <ContentControl x:Name=\"DesignerPresenter\" Grid.Row=\"1\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl",
                    "    x:Class=\"Demo.Snippets.ExampleHost\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Grid>",
                    "    <Grid.RowDefinitions>",
                    "      <RowDefinition Height=\"Auto\" />",
                    "      <RowDefinition Height=\"*\" />",
                    "    </Grid.RowDefinitions>",
                    "    <StackPanel Margin=\"0,0,0,12\">",
                    "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                    "                 FontSize=\"20\"",
                    "                 Text=\"Reusable ReportDesigner\" />",
                    "      <TextBlock Margin=\"0,6,0,0\"",
                    "                 TextWrapping=\"Wrap\"",
                    "                 Text=\"Keep the shell native and host one browser-based report designer on top of the neutral WebHost.\" />",
                    "    </StackPanel>",
                    "    <ContentControl x:Name=\"DesignerPresenter\" Grid.Row=\"1\" />",
                    "  </Grid>",
                    "</UserControl>",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "<UserControl",
                "    x:Class=\"Demo.Snippets.ExampleHost\"",
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                "  <Grid>",
                "    <Grid.RowDefinitions>",
                "      <RowDefinition Height=\"Auto\" />",
                "      <RowDefinition Height=\"*\" />",
                "    </Grid.RowDefinitions>",
                "    <StackPanel Margin=\"0,0,0,12\">",
                "      <TextBlock FontFamily=\"Bahnschrift SemiBold\"",
                "                 FontSize=\"20\"",
                "                 Text=\"Reusable ReportDesigner\" />",
                "      <TextBlock Margin=\"0,6,0,0\"",
                "                 TextWrapping=\"Wrap\"",
                "                 Text=\"Keep the shell native and host one browser-based report designer on top of the neutral WebHost.\" />",
                "    </StackPanel>",
                "    <ContentControl x:Name=\"DesignerPresenter\" Grid.Row=\"1\" />",
                "  </Grid>",
                "</UserControl>",
            });
        }

        private static string BuildReportDesignerHostFile(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "using Avalonia.Controls;",
                    "using Avalonia.VisualTree;",
                    "using PhialeTech.Components.Shared.Services;",
                    "using PhialeTech.ReportDesigner.Abstractions;",
                    "using PhialeTech.ReportDesigner.Avalonia.Controls;",
                    "using PhialeTech.WebHost.Avalonia;",
                    "using System;",
                    string.Empty,
                    "namespace Demo.Snippets",
                    "{",
                    "    public sealed partial class ExampleHost : UserControl",
                    "    {",
                    "        private readonly PhialeReportDesigner _designer;",
                    "        private bool _opened;",
                    string.Empty,
                    "        public ExampleHost()",
                    "        {",
                    "            InitializeComponent();",
                    "            _designer = new PhialeReportDesigner(",
                    "                new AvaloniaWebComponentHostFactory(),",
                    "                new ReportDesignerOptions",
                    "                {",
                    "                    InitialLocale = \"en\",",
                    "                    InitialTheme = \"light\"",
                    "                });",
                    "            DesignerPresenter.Content = _designer;",
                    "            AttachedToVisualTree += HandleAttachedToVisualTree;",
                    "            DetachedFromVisualTree += HandleDetachedFromVisualTree;",
                    "        }",
                    string.Empty,
                    "        private async void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)",
                    "        {",
                    "            if (_opened)",
                    "            {",
                    "                return;",
                    "            }",
                    string.Empty,
                    "            _opened = true;",
                    "            await _designer.SetDataSchemaAsync(DemoReportDesignerSampleBuilder.CreateSchema());",
                    "            await _designer.SetSampleDataAsync(DemoReportDesignerSampleBuilder.CreateSampleDataJson());",
                    "            await _designer.SetReportDataAsync(DemoReportDesignerSampleBuilder.CreateReportDataJson());",
                    "            await _designer.LoadDefinitionAsync(DemoReportDesignerSampleBuilder.CreateDefinition());",
                    "            await _designer.SetModeAsync(ReportDesignerMode.Design);",
                    "        }",
                    string.Empty,
                    "        private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)",
                    "        {",
                    "            _designer.Dispose();",
                    "        }",
                    "    }",
                    "}",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "using Microsoft.UI.Xaml;",
                    "using Microsoft.UI.Xaml.Controls;",
                    "using PhialeTech.Components.Shared.Services;",
                    "using PhialeTech.ReportDesigner.Abstractions;",
                    "using PhialeTech.ReportDesigner.WinUI.Controls;",
                    "using PhialeTech.WebHost.WinUI;",
                    "using System;",
                    string.Empty,
                    "namespace Demo.Snippets",
                    "{",
                    "    public sealed partial class ExampleHost : UserControl",
                    "    {",
                    "        private readonly PhialeReportDesigner _designer;",
                    "        private bool _opened;",
                    string.Empty,
                    "        public ExampleHost()",
                    "        {",
                    "            InitializeComponent();",
                    "            _designer = new PhialeReportDesigner(",
                    "                new WinUiWebComponentHostFactory(),",
                    "                new ReportDesignerOptions",
                    "                {",
                    "                    InitialLocale = \"en\",",
                    "                    InitialTheme = \"light\"",
                    "                });",
                    "            DesignerPresenter.Content = _designer;",
                    "            Loaded += HandleLoaded;",
                    "            Unloaded += HandleUnloaded;",
                    "        }",
                    string.Empty,
                    "        private async void HandleLoaded(object sender, RoutedEventArgs e)",
                    "        {",
                    "            if (_opened)",
                    "            {",
                    "                return;",
                    "            }",
                    string.Empty,
                    "            _opened = true;",
                    "            await _designer.SetDataSchemaAsync(DemoReportDesignerSampleBuilder.CreateSchema());",
                    "            await _designer.SetSampleDataAsync(DemoReportDesignerSampleBuilder.CreateSampleDataJson());",
                    "            await _designer.SetReportDataAsync(DemoReportDesignerSampleBuilder.CreateReportDataJson());",
                    "            await _designer.LoadDefinitionAsync(DemoReportDesignerSampleBuilder.CreateDefinition());",
                    "            await _designer.SetModeAsync(ReportDesignerMode.Design);",
                    "        }",
                    string.Empty,
                    "        private void HandleUnloaded(object sender, RoutedEventArgs e)",
                    "        {",
                    "            _designer.Dispose();",
                    "        }",
                    "    }",
                    "}",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.Components.Shared.Services;",
                "using PhialeTech.ReportDesigner.Abstractions;",
                "using PhialeTech.ReportDesigner.Wpf.Controls;",
                "using PhialeTech.WebHost.Wpf;",
                "using System;",
                "using System.Windows;",
                "using System.Windows.Controls;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public sealed partial class ExampleHost : UserControl",
                "    {",
                "        private readonly PhialeReportDesigner _designer;",
                "        private bool _opened;",
                string.Empty,
                "        public ExampleHost()",
                "        {",
                "            InitializeComponent();",
                "            _designer = new PhialeReportDesigner(",
                "                new WpfWebComponentHostFactory(),",
                "                new ReportDesignerOptions",
                "                {",
                "                    InitialLocale = \"en\",",
                "                    InitialTheme = \"light\"",
                "                });",
                "            DesignerPresenter.Content = _designer;",
                "            Loaded += HandleLoaded;",
                "            Unloaded += HandleUnloaded;",
                "        }",
                string.Empty,
                "        private async void HandleLoaded(object sender, RoutedEventArgs e)",
                "        {",
                "            if (_opened)",
                "            {",
                "                return;",
                "            }",
                string.Empty,
                "            _opened = true;",
                "            await _designer.SetDataSchemaAsync(DemoReportDesignerSampleBuilder.CreateSchema());",
                "            await _designer.SetSampleDataAsync(DemoReportDesignerSampleBuilder.CreateSampleDataJson());",
                "            await _designer.SetReportDataAsync(DemoReportDesignerSampleBuilder.CreateReportDataJson());",
                "            await _designer.LoadDefinitionAsync(DemoReportDesignerSampleBuilder.CreateDefinition());",
                "            await _designer.SetModeAsync(ReportDesignerMode.Design);",
                "        }",
                string.Empty,
                "        private void HandleUnloaded(object sender, RoutedEventArgs e)",
                "        {",
                "            _designer.Dispose();",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildReportDesignerSampleFile()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.ReportDesigner.Abstractions;",
                "using System.Collections.Generic;",
                "using System.Text.Json;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public static class DemoReportDesignerSampleBuilder",
                "    {",
                "        public static ReportDefinition CreateDefinition()",
                "        {",
                "            return new ReportDefinition",
                "            {",
                "                Version = 1,",
                "                Page = new ReportPageSettings { Size = \"A4\", Orientation = \"Portrait\", Margin = \"18mm\" },",
                "                Blocks = new List<ReportBlockDefinition>",
                "                {",
                "                    new ReportBlockDefinition",
                "                    {",
                "                        Type = \"Text\",",
                "                        Name = \"Invoice title\",",
                "                        Text = \"Sales invoice\",",
                "                        Style = new ReportBlockStyle { FontSize = \"28px\", FontWeight = \"700\", Margin = \"0 0 14px 0\" }",
                "                    },",
                "                    new ReportBlockDefinition",
                "                    {",
                "                        Type = \"Columns\",",
                "                        Name = \"Invoice header columns\",",
                "                        ColumnCount = 2,",
                "                        ColumnGap = \"18px\",",
                "                        KeepTogether = true,",
                "                        Children = new List<ReportBlockDefinition>",
                "                        {",
                "                            new ReportBlockDefinition",
                "                            {",
                "                                Type = \"Container\",",
                "                                Name = \"Left column\",",
                "                                Children = new List<ReportBlockDefinition>",
                "                                {",
                "                                    new ReportBlockDefinition",
                "                                    {",
                "                                        Type = \"FieldList\",",
                "                                        Name = \"Invoice details\",",
                "                                        Fields = new List<ReportFieldListItemDefinition>",
                "                                        {",
                "                                            new ReportFieldListItemDefinition { Label = \"Invoice number\", Binding = \"InvoiceNumber\" },",
                "                                            new ReportFieldListItemDefinition { Label = \"Invoice date\", Binding = \"InvoiceDate\", Format = new ReportValueFormat { Kind = \"date\", Pattern = \"yyyy-MM-dd\" } }",
                "                                        }",
                "                                    }",
                "                                }",
                "                            },",
                "                            new ReportBlockDefinition",
                "                            {",
                "                                Type = \"Container\",",
                "                                Name = \"Right column\",",
                "                                Children = new List<ReportBlockDefinition>",
                "                                {",
                "                                    new ReportBlockDefinition",
                "                                    {",
                "                                        Type = \"SpecialField\",",
                "                                        Name = \"Current date\",",
                "                                        SpecialFieldKind = ReportSpecialFieldKinds.CurrentDate,",
                "                                        Format = new ReportValueFormat { Kind = \"date\", Pattern = \"yyyy-MM-dd\" }",
                "                                    },",
                "                                    new ReportBlockDefinition",
                "                                    {",
                "                                        Type = \"FieldList\",",
                "                                        Name = \"Buyer\",",
                "                                        Fields = new List<ReportFieldListItemDefinition>",
                "                                        {",
                "                                            new ReportFieldListItemDefinition { Label = \"Buyer\", Binding = \"Buyer.Name\" },",
                "                                            new ReportFieldListItemDefinition { Label = \"Tax ID\", Binding = \"Buyer.TaxId\" }",
                "                                        }",
                "                                    }",
                "                                }",
                "                            }",
                "                        }",
                "                    },",
                "                    new ReportBlockDefinition",
                "                    {",
                "                        Type = \"Table\",",
                "                        Name = \"Invoice lines\",",
                "                        ItemsSource = \"Items\",",
                "                        RepeatHeader = true,",
                "                        Columns = new List<ReportTableColumnDefinition>",
                "                        {",
                "                            new ReportTableColumnDefinition { Header = \"Name\", Binding = \"Name\", Width = \"52%\" },",
                "                            new ReportTableColumnDefinition { Header = \"Quantity\", Binding = \"Quantity\", Kind = \"number\", Width = \"16%\", TextAlign = \"right\" },",
                "                            new ReportTableColumnDefinition { Header = \"Net\", Binding = \"LineTotal\", Kind = \"currency\", Currency = \"PLN\", Decimals = 2, Width = \"32%\", TextAlign = \"right\" }",
                "                        }",
                "                    },",
                "                    new ReportBlockDefinition",
                "                    {",
                "                        Type = \"SpecialField\",",
                "                        Name = \"Page X of Y\",",
                "                        SpecialFieldKind = ReportSpecialFieldKinds.PageNumberOfTotalPages,",
                "                        PageBreakBefore = true",
                "                    },",
                "                }",
                "            };",
                "        }",
                string.Empty,
                "        public static ReportDataSchema CreateSchema()",
                "        {",
                "            return new ReportDataSchema",
                "            {",
                "                Fields = new List<ReportDataFieldDefinition>",
                "                {",
                    "                    new ReportDataFieldDefinition { Name = \"InvoiceNumber\", Type = \"string\" },",
                "                    new ReportDataFieldDefinition { Name = \"InvoiceDate\", Type = \"date\" },",
                "                    new ReportDataFieldDefinition",
                "                    {",
                "                        Name = \"Buyer\",",
                "                        Type = \"object\",",
                "                        Children = new List<ReportDataFieldDefinition>",
                "                        {",
                "                            new ReportDataFieldDefinition { Name = \"Name\", Type = \"string\" },",
                "                            new ReportDataFieldDefinition { Name = \"TaxId\", Type = \"string\" }",
                "                        }",
                "                    },",
                "                    new ReportDataFieldDefinition { Name = \"Items\", Type = \"object\", IsCollection = true },",
                "                }",
                "            };",
                "        }",
                string.Empty,
                "        public static string CreateSampleDataJson()",
                "        {",
                "            return JsonSerializer.Serialize(new",
                "            {",
                "                InvoiceNumber = \"SAMPLE-2026-001\",",
                "                InvoiceDate = \"2026-03-28\",",
                "                Buyer = new { Name = \"Sample buyer\", TaxId = \"PL1234567890\" },",
                "                Items = new[] { new { Name = \"Printer\", Quantity = 2, LineTotal = 1200.00m } },",
                "            });",
                "        }",
                string.Empty,
                "        public static string CreateReportDataJson()",
                "        {",
                "            return JsonSerializer.Serialize(new",
                "            {",
                "                InvoiceNumber = \"INV-2026-0418\",",
                "                InvoiceDate = \"2026-03-28\",",
                "                Buyer = new { Name = \"GeoField Logistics\", TaxId = \"PL9876543210\" },",
                "                Items = new[] { new { Name = \"GNSS antenna\", Quantity = 2, LineTotal = 1890.00m } },",
                "            });",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildMonacoEditorMarkup()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "<Grid>",
                "  <local:MonacoEditorHost />",
                "</Grid>",
            });
        }

        private static string BuildMonacoEditorHostFile()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using System.Windows.Controls;",
                "using PhialeTech.MonacoEditor.Wpf.Controls;",
                string.Empty,
                "namespace Demo",
                "{",
                "    public sealed class MonacoEditorHost : UserControl",
                "    {",
                "        private readonly PhialeMonacoEditor _editor;",
                string.Empty,
                "        public MonacoEditorHost()",
                "        {",
                "            _editor = new PhialeMonacoEditor();",
                "            Content = _editor;",
                "            Loaded += async (_, __) =>",
                "            {",
                "                await _editor.InitializeAsync();",
                "                await _editor.SetLanguageAsync(\"yaml\");",
                "                await _editor.SetThemeAsync(\"light\");",
                "                await _editor.SetValueAsync(\"id: demo\\nname: Monaco editor\");",
                "            };",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildWebHostHtmlFile()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "<!doctype html>",
                "<html>",
                "<head>",
                "  <meta charset=\"utf-8\" />",
                "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />",
                "  <title>WebHost sample entry</title>",
                "</head>",
                "<body>",
                "  <button id=\"ping\">Ping host</button>",
                "  <pre id=\"log\"></pre>",
                "  <script>",
                "    (function () {",
                "      var log = document.getElementById('log');",
                "      function write(message) {",
                "        log.textContent = message + '\\n' + log.textContent;",
                "      }",
                "",
                "      window.addEventListener('phiale-webhost-bridge-ready', function () {",
                "        write('bridge ready');",
                "        if (window.PhialeWebHost) {",
                "          window.PhialeWebHost.postMessage({",
                "            type: 'demoReady',",
                "            detail: 'browser side is ready'",
                "          });",
                "        }",
                "      });",
                "",
                "      window.addEventListener('phiale-webhost-message', function (event) {",
                "        write('host -> js: ' + JSON.stringify(event.detail));",
                "      });",
                "",
                "      document.getElementById('ping').addEventListener('click', function () {",
                "        if (window.PhialeWebHost) {",
                "          window.PhialeWebHost.postMessage({",
                "            type: 'demoPing',",
                "            detail: 'button click from JS'",
                "          });",
                "        }",
                "      });",
                "    })();",
                "  </script>",
                "</body>",
                "</html>",
            });
        }

        private static string BuildDefinitionManagerMarkup(PlatformSpec spec)
        {
            var builder = new StringBuilder();
            builder.AppendLine(spec.MarkupHeader);
            builder.AppendLine("  <StackPanel Margin=\"18\">");
            builder.AppendLine("    <TextBlock FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("               FontSize=\"24\"");
            builder.AppendLine("               Text=\"{Binding PageDefinitionTitle}\" />");
            builder.AppendLine("    <TextBlock Margin=\"0,8,0,18\"");
            builder.AppendLine("               FontSize=\"14\"");
            builder.AppendLine("               TextWrapping=\"Wrap\"");
            builder.AppendLine("               Text=\"{Binding PageDefinitionSummary}\" />");
            builder.AppendLine("    <Border Padding=\"14\"");
            builder.AppendLine("            BorderThickness=\"1\">");
            builder.AppendLine("      <StackPanel>");
            builder.AppendLine("        <TextBlock FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                   Text=\"Resolved definition\" />");
            builder.AppendLine("        <TextBlock Margin=\"0,6,0,0\"");
            builder.AppendLine("                   Text=\"{Binding GroupingDefinitionKey}\" />");
            builder.AppendLine("        <TextBlock Margin=\"0,4,0,0\"");
            builder.AppendLine("                   Text=\"{Binding GroupingDefinitionSourceId}\" />");
            builder.AppendLine("        <TextBlock Margin=\"0,10,0,0\"");
            builder.AppendLine("                   FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                   Text=\"{Binding GroupingDefinitionTitle}\" />");
            builder.AppendLine("        <TextBlock Margin=\"0,4,0,0\"");
            builder.AppendLine("                   TextWrapping=\"Wrap\"");
            builder.AppendLine("                   Text=\"{Binding GroupingDefinitionSummary}\" />");
            builder.AppendLine("      </StackPanel>");
            builder.AppendLine("    </Border>");
            builder.AppendLine("  </StackPanel>");
            builder.AppendLine(spec.MarkupFooter);
            return builder.ToString().TrimEnd();
        }

        private static string BuildFoundationsMarkup(PlatformSpec spec)
        {
            var builder = new StringBuilder();
            builder.AppendLine(spec.MarkupHeader);
            builder.AppendLine("  <ScrollViewer VerticalScrollBarVisibility=\"Auto\">");
            builder.AppendLine("    <StackPanel Margin=\"18\">");
            builder.AppendLine("      <TextBlock FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                 FontSize=\"24\"");
            builder.AppendLine("                 Text=\"What is already defined\" />");
            builder.AppendLine("      <TextBlock Margin=\"0,8,0,16\"");
            builder.AppendLine("                 FontSize=\"14\"");
            builder.AppendLine("                 TextWrapping=\"Wrap\"");
            builder.AppendLine("                 Text=\"This page turns the current demo shell decisions into named foundations: text roles, semantic brushes, surfaces and repeated layout constants.\" />");
            builder.AppendLine("      <ItemsControl ItemsSource=\"{Binding TypographyTokens}\">");
            builder.AppendLine("        <ItemsControl.ItemTemplate>");
            builder.AppendLine("          <DataTemplate>");
            builder.AppendLine("            <Border Margin=\"0,0,0,12\"");
            builder.AppendLine("                    Padding=\"14\"");
            builder.AppendLine("                    BorderThickness=\"1\">");
            builder.AppendLine("              <Grid>");
            builder.AppendLine("                <Grid.ColumnDefinitions>");
            builder.AppendLine("                  <ColumnDefinition Width=\"240\" />");
            builder.AppendLine("                  <ColumnDefinition Width=\"*\" />");
            builder.AppendLine("                </Grid.ColumnDefinitions>");
            builder.AppendLine("                <StackPanel>");
            builder.AppendLine("                  <TextBlock FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                             FontSize=\"12\"");
            builder.AppendLine("                             Text=\"{Binding TokenName}\" />");
            builder.AppendLine("                  <TextBlock Margin=\"0,4,0,0\"");
            builder.AppendLine("                             FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                             FontSize=\"14\"");
            builder.AppendLine("                             Text=\"{Binding Role}\" />");
            builder.AppendLine("                  <TextBlock Margin=\"0,6,0,0\"");
            builder.AppendLine("                             FontSize=\"12\"");
            builder.AppendLine("                             TextWrapping=\"Wrap\"");
            builder.AppendLine("                             Text=\"{Binding Usage}\" />");
            builder.AppendLine("                </StackPanel>");
            builder.AppendLine("                <StackPanel Grid.Column=\"1\" Margin=\"18,0,0,0\">");
            builder.AppendLine("                  <TextBlock FontFamily=\"{Binding FontFamilyName}\"");
            builder.AppendLine("                             FontSize=\"{Binding FontSize}\"");
            builder.AppendLine("                             FontWeight=\"{Binding FontWeight}\"");
            builder.AppendLine("                             Text=\"{Binding SampleText}\" />");
            builder.AppendLine("                  <TextBlock Margin=\"0,8,0,0\"");
            builder.AppendLine("                             FontSize=\"11\"");
            builder.AppendLine("                             Text=\"{Binding StyleSummary}\" />");
            builder.AppendLine("                </StackPanel>");
            builder.AppendLine("              </Grid>");
            builder.AppendLine("            </Border>");
            builder.AppendLine("          </DataTemplate>");
            builder.AppendLine("        </ItemsControl.ItemTemplate>");
            builder.AppendLine("      </ItemsControl>");
            builder.AppendLine("      <TextBlock Margin=\"0,10,0,10\"");
            builder.AppendLine("                 FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                 FontSize=\"18\"");
            builder.AppendLine("                 Text=\"Semantic brushes\" />");
            builder.AppendLine("      <ItemsControl ItemsSource=\"{Binding SurfaceTokens}\">");
            builder.AppendLine("        <ItemsControl.ItemTemplate>");
            builder.AppendLine("          <DataTemplate>");
            builder.AppendLine("            <Grid Margin=\"0,0,0,8\">");
            builder.AppendLine("              <Grid.ColumnDefinitions>");
            builder.AppendLine("                <ColumnDefinition Width=\"240\" />");
            builder.AppendLine("                <ColumnDefinition Width=\"*\" />");
            builder.AppendLine("                <ColumnDefinition Width=\"120\" />");
            builder.AppendLine("                <ColumnDefinition Width=\"120\" />");
            builder.AppendLine("              </Grid.ColumnDefinitions>");
            builder.AppendLine("              <TextBlock FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                         FontSize=\"12\"");
            builder.AppendLine("                         Text=\"{Binding TokenName}\" />");
            builder.AppendLine("              <TextBlock Grid.Column=\"1\"");
            builder.AppendLine("                         Margin=\"12,0,0,0\"");
            builder.AppendLine("                         FontSize=\"12\"");
            builder.AppendLine("                         TextWrapping=\"Wrap\"");
            builder.AppendLine("                         Text=\"{Binding Usage}\" />");
            builder.AppendLine("              <Border Grid.Column=\"2\"");
            builder.AppendLine("                      Width=\"88\"");
            builder.AppendLine("                      Height=\"24\"");
            builder.AppendLine("                      Margin=\"12,0,0,0\"");
            builder.AppendLine("                      Background=\"{Binding DayHex}\"");
            builder.AppendLine("                      CornerRadius=\"6\">");
            builder.AppendLine("                <TextBlock HorizontalAlignment=\"Center\"");
            builder.AppendLine("                           VerticalAlignment=\"Center\"");
            builder.AppendLine("                           FontSize=\"10\"");
            builder.AppendLine("                           Text=\"{Binding DayHex}\" />");
            builder.AppendLine("              </Border>");
            builder.AppendLine("              <Border Grid.Column=\"3\"");
            builder.AppendLine("                      Width=\"88\"");
            builder.AppendLine("                      Height=\"24\"");
            builder.AppendLine("                      Margin=\"12,0,0,0\"");
            builder.AppendLine("                      Background=\"{Binding NightHex}\"");
            builder.AppendLine("                      CornerRadius=\"6\">");
            builder.AppendLine("                <TextBlock HorizontalAlignment=\"Center\"");
            builder.AppendLine("                           VerticalAlignment=\"Center\"");
            builder.AppendLine("                           FontSize=\"10\"");
            builder.AppendLine("                           Text=\"{Binding NightHex}\" />");
            builder.AppendLine("              </Border>");
            builder.AppendLine("            </Grid>");
            builder.AppendLine("          </DataTemplate>");
            builder.AppendLine("        </ItemsControl.ItemTemplate>");
            builder.AppendLine("      </ItemsControl>");
            builder.AppendLine("      <TextBlock Margin=\"0,18,0,10\"");
            builder.AppendLine("                 FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                 FontSize=\"18\"");
            builder.AppendLine("                 Text=\"Rhythm and shape\" />");
            builder.AppendLine("      <ItemsControl ItemsSource=\"{Binding MeasureTokens}\">");
            builder.AppendLine("        <ItemsControl.ItemTemplate>");
            builder.AppendLine("          <DataTemplate>");
            builder.AppendLine("            <Grid Margin=\"0,0,0,8\">");
            builder.AppendLine("              <Grid.ColumnDefinitions>");
            builder.AppendLine("                <ColumnDefinition Width=\"140\" />");
            builder.AppendLine("                <ColumnDefinition Width=\"100\" />");
            builder.AppendLine("                <ColumnDefinition Width=\"*\" />");
            builder.AppendLine("              </Grid.ColumnDefinitions>");
            builder.AppendLine("              <TextBlock FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                         FontSize=\"12\"");
            builder.AppendLine("                         Text=\"{Binding TokenName}\" />");
            builder.AppendLine("              <TextBlock Grid.Column=\"1\"");
            builder.AppendLine("                         Text=\"{Binding Value}\" />");
            builder.AppendLine("              <TextBlock Grid.Column=\"2\"");
            builder.AppendLine("                         Margin=\"12,0,0,0\"");
            builder.AppendLine("                         TextWrapping=\"Wrap\"");
            builder.AppendLine("                         Text=\"{Binding Usage}\" />");
            builder.AppendLine("            </Grid>");
            builder.AppendLine("          </DataTemplate>");
            builder.AppendLine("        </ItemsControl.ItemTemplate>");
            builder.AppendLine("      </ItemsControl>");
            builder.AppendLine("      <TextBlock Margin=\"0,18,0,10\"");
            builder.AppendLine("                 FontFamily=\"Bahnschrift SemiBold\"");
            builder.AppendLine("                 FontSize=\"18\"");
            builder.AppendLine("                 Text=\"Buttons / Actions\" />");
            builder.AppendLine("      <TextBlock FontSize=\"13\"");
            builder.AppendLine("                 TextWrapping=\"Wrap\"");
            builder.AppendLine("                 Text=\"Buttons should expose a clear action hierarchy, predictable focus treatment and stable sizing before any YAML-specific semantics are applied.\" />");
            builder.AppendLine("      <WrapPanel Margin=\"0,12,0,0\">");
            builder.AppendLine("        <Button Style=\"{StaticResource CommitToolbarButtonStyle}\" Content=\"Primary action\" />");
            builder.AppendLine("        <Button Style=\"{StaticResource ToolbarButtonStyle}\" Content=\"Secondary action\" />");
            builder.AppendLine("        <Button Style=\"{StaticResource CancelToolbarButtonStyle}\" Content=\"Danger action\" />");
            builder.AppendLine("      </WrapPanel>");
            builder.AppendLine("      <WrapPanel Margin=\"0,12,0,0\">");
            builder.AppendLine("        <Button Style=\"{StaticResource CommitToolbarButtonStyle}\" Content=\"Enabled primary\" />");
            builder.AppendLine("        <Button Style=\"{StaticResource ToolbarButtonStyle}\" Content=\"Enabled secondary\" />");
            builder.AppendLine("        <Button Style=\"{StaticResource CancelToolbarButtonStyle}\" Content=\"Enabled danger\" />");
            builder.AppendLine("        <Button Style=\"{StaticResource CommitToolbarButtonStyle}\" IsEnabled=\"False\" Content=\"Disabled primary\" />");
            builder.AppendLine("        <Button Style=\"{StaticResource ToolbarButtonStyle}\" IsEnabled=\"False\" Content=\"Disabled secondary\" />");
            builder.AppendLine("      </WrapPanel>");
            builder.AppendLine("    </StackPanel>");
            builder.AppendLine("  </ScrollViewer>");
            builder.AppendLine(spec.MarkupFooter);
            return builder.ToString().TrimEnd();
        }

        private static string BuildFoundationsViewModel()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using System.Collections.Generic;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "public sealed class FoundationsExampleViewModel",
                "{",
                "    public IReadOnlyList<TypographyToken> TypographyTokens { get; } = new[]",
                "    {",
                "        new TypographyToken(\"Text.Hero\", \"Hero / page headline\", \"Primary selected-example headline and the strongest entry point.\", \"Grid | Foundations\", \"Bahnschrift SemiBold\", 34d, \"SemiBold\", \"Bahnschrift SemiBold · 34\"),",
                "        new TypographyToken(\"Text.Display\", \"Display / overview\", \"Overview headlines and larger section entries.\", \"Select an example\", \"Bahnschrift SemiBold\", 30d, \"SemiBold\", \"Bahnschrift SemiBold · 30\"),",
                "        new TypographyToken(\"Text.Section\", \"Section title\", \"Section headlines, cards and stronger narrative blocks.\", \"Typography and roles\", \"Bahnschrift SemiBold\", 18d, \"SemiBold\", \"Bahnschrift SemiBold · 18\"),",
                "        new TypographyToken(\"Text.ControlTitle\", \"Control title / compact header\", \"Popup titles, calendar month captions and compact embedded headers.\", \"April 2026\", \"Bahnschrift SemiBold\", 22d, \"SemiBold\", \"Bahnschrift SemiBold · 22\"),",
                "        new TypographyToken(\"Text.ControlLabel\", \"Control label / choice\", \"Weekday labels, day values and compact choice labels inside controls.\", \"14\", \"Bahnschrift SemiBold\", 15d, \"SemiBold\", \"Bahnschrift SemiBold · 15\"),",
                "        new TypographyToken(\"Text.Label\", \"Label / chrome\", \"Field labels, tab chrome and compact navigation.\", \"Theme\", \"Bahnschrift SemiBold\", 14d, \"SemiBold\", \"Bahnschrift SemiBold · 14\"),",
                "        new TypographyToken(\"Text.Body\", \"Body copy\", \"Primary descriptions and scenario copy.\", \"This is the main explanatory layer and should not compete with the headline.\", \"Segoe UI\", 15d, \"Normal\", \"Segoe UI · 15\"),",
                "        new TypographyToken(\"Text.Support\", \"Support text\", \"Hints, toolbar status and supportive metadata.\", \"Use this tone for supportive commentary and second-plane metadata.\", \"Segoe UI\", 13d, \"Normal\", \"Segoe UI · 13\"),",
                "        new TypographyToken(\"Text.Code\", \"Code\", \"Code tab and technical snippets.\", \"new GridColumnDefinition(\\\"ObjectName\\\", \\\"Object name\\\")\", \"Consolas\", 13d, \"Normal\", \"Consolas · 13\"),",
                "    };",
                string.Empty,
                "    public IReadOnlyList<ColorToken> SurfaceTokens { get; } = new[]",
                "    {",
                "        new ColorToken(\"DemoPanelBackgroundBrush\", \"Primary card and panel surface.\", \"#FFFFFF\", \"#171C25\"),",
                "        new ColorToken(\"DemoHintBackgroundBrush\", \"Hints and gentle callouts.\", \"#F6FAF9\", \"#1A2431\"),",
                "        new ColorToken(\"DemoCodeBackgroundBrush\", \"Code surface.\", \"#131A22\", \"#0F141C\"),",
                "    };",
                string.Empty,
                "    public IReadOnlyList<MeasureToken> MeasureTokens { get; } = new[]",
                "    {",
                "        new MeasureToken(\"Radius.14\", \"14\", \"Large overview cards, code surface and stronger containers.\"),",
                "        new MeasureToken(\"Radius.6\", \"6\", \"Buttons, inputs, popups and interactive controls.\"),",
                "        new MeasureToken(\"Space.18\", \"18\", \"Primary surface padding and code preview inset.\"),",
                "        new MeasureToken(\"Space.10\", \"10\", \"Inline gaps, chip rhythm and compact margins.\"),",
                "    };",
                string.Empty,
                "    public sealed class TypographyToken",
                "    {",
                "        public TypographyToken(string tokenName, string role, string usage, string sampleText, string fontFamilyName, double fontSize, string fontWeight, string styleSummary)",
                "        {",
                "            TokenName = tokenName;",
                "            Role = role;",
                "            Usage = usage;",
                "            SampleText = sampleText;",
                "            FontFamilyName = fontFamilyName;",
                "            FontSize = fontSize;",
                "            FontWeight = fontWeight;",
                "            StyleSummary = styleSummary;",
                "        }",
                string.Empty,
                "        public string TokenName { get; }",
                "        public string Role { get; }",
                "        public string Usage { get; }",
                "        public string SampleText { get; }",
                "        public string FontFamilyName { get; }",
                "        public double FontSize { get; }",
                "        public string FontWeight { get; }",
                "        public string StyleSummary { get; }",
                "    }",
                string.Empty,
                "    public sealed class ColorToken",
                "    {",
                "        public ColorToken(string tokenName, string usage, string dayHex, string nightHex)",
                "        {",
                "            TokenName = tokenName;",
                "            Usage = usage;",
                "            DayHex = dayHex;",
                "            NightHex = nightHex;",
                "        }",
                string.Empty,
                "        public string TokenName { get; }",
                "        public string Usage { get; }",
                "        public string DayHex { get; }",
                "        public string NightHex { get; }",
                "    }",
                string.Empty,
                "    public sealed class MeasureToken",
                "    {",
                "        public MeasureToken(string tokenName, string value, string usage)",
                "        {",
                "            TokenName = tokenName;",
                "            Value = value;",
                "            Usage = usage;",
                "        }",
                string.Empty,
                "        public string TokenName { get; }",
                "        public string Value { get; }",
                "        public string Usage { get; }",
                "    }",
                "}",
                "}",
            });
        }

        private static string BuildFoundationsHostFile(PlatformSpec spec)
        {
            if (!spec.WrapHostInClass)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, new[]
            {
                "using System.Windows.Controls;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public sealed partial class ExampleHost : UserControl",
                "    {",
                "        public FoundationsExampleViewModel ViewModel { get; } = new FoundationsExampleViewModel();",
                string.Empty,
                "        public ExampleHost()",
                "        {",
                "            InitializeComponent();",
                "            DataContext = ViewModel;",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildActiveLayerSelectorMarkup(string platformKey)
        {
            if (string.Equals(platformKey, "Avalonia", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl xmlns=\"https://github.com/avaloniaui\"",
                    "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "             xmlns:selector=\"clr-namespace:PhialeTech.ActiveLayerSelector.Avalonia.Controls;assembly=PhialeTech.ActiveLayerSelector.Avalonia\">",
                    "  <selector:ActiveLayerSelector State=\"{Binding ActiveLayerSelectorState}\"",
                    "                                LanguageCode=\"{Binding LanguageCode}\"",
                    "                                InitialVisibleItemCount=\"5\" />",
                    "</UserControl>",
                });
            }

            if (string.Equals(platformKey, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platformKey, "WinUi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "<UserControl",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "    xmlns:selector=\"using:PhialeTech.ActiveLayerSelector.WinUI.Controls\">",
                    "  <selector:ActiveLayerSelector State=\"{Binding ActiveLayerSelectorState}\"",
                    "                                LanguageCode=\"{Binding LanguageCode}\"",
                    "                                InitialVisibleItemCount=\"5\" />",
                    "</UserControl>",
                });
            }

            return string.Join(Environment.NewLine, new[]
            {
                "<UserControl",
                "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                "    xmlns:selector=\"clr-namespace:PhialeTech.ActiveLayerSelector.Wpf.Controls;assembly=PhialeTech.ActiveLayerSelector.Wpf\">",
                "  <selector:ActiveLayerSelector State=\"{Binding ActiveLayerSelectorState}\"",
                "                                LanguageCode=\"{Binding LanguageCode}\"",
                "                                InitialVisibleItemCount=\"5\" />",
                "</UserControl>",
            });
        }

        private static string BuildActiveLayerSelectorViewModel()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.ActiveLayerSelector;",
                string.Empty,
                "public sealed class ActiveLayerSelectorExampleViewModel",
                "{",
                "    public string LanguageCode { get; } = \"en\";",
                string.Empty,
                "    public IActiveLayerSelectorState ActiveLayerSelectorState { get; } =",
                "        DemoActiveLayerSelectorFactory.CreateDefaultState();",
                "}",
            });
        }

        private static string BuildActiveLayerSelectorState()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.ActiveLayerSelector;",
                string.Empty,
                "public static class DemoActiveLayerSelectorFactory",
                "{",
                "    public static IActiveLayerSelectorState CreateDefaultState()",
                "    {",
                "        return new DemoActiveLayerSelectorState(new[]",
                "        {",
                "            new ActiveLayerSelectorItemState",
                "            {",
                "                LayerId = \"roads\",",
                "                Name = \"Roads\",",
                "                TreePath = \"Operational / Transport\",",
                "                LayerType = \"PostGIS\",",
                "                GeometryType = \"LineString\",",
                "                IsActive = true,",
                "                IsVisible = true,",
                "                IsSelectable = true,",
                "                IsEditable = true,",
                "                IsSnappable = true,",
                "            },",
                "            new ActiveLayerSelectorItemState",
                "            {",
                "                LayerId = \"buildings\",",
                "                Name = \"Buildings\",",
                "                TreePath = \"Operational / Base\",",
                "                LayerType = \"SHP\",",
                "                GeometryType = \"Polygon\",",
                "                IsVisible = true,",
                "                IsSelectable = true,",
                "                IsEditable = false,",
                "                IsSnappable = true,",
                "            },",
                "        });",
                "    }",
                "}",
            });
        }

        private static string BuildMarkup(PlatformSpec spec, string exampleId)
        {
            var markup = new StringBuilder();
            markup.AppendLine(spec.MarkupHeader);
            markup.AppendLine("  <StackPanel>");

            foreach (var line in BuildToolbarMarkup(exampleId))
            {
                markup.AppendLine("    " + line);
            }

            markup.AppendLine("    <" + spec.GridTag + " x:Name=\"DemoGrid\"");
            markup.AppendLine("        EditSessionContext=\"{Binding GridEditSessionContext}\"");

            if (UsesGroupsBinding(exampleId))
            {
                markup.AppendLine("        Groups=\"{Binding GridGroups, Mode=TwoWay}\"");
            }

            if (UsesSortsBinding(exampleId))
            {
                markup.AppendLine("        Sorts=\"{Binding GridSorts, Mode=TwoWay}\"");
            }

            if (UsesSummariesBinding(exampleId))
            {
                markup.AppendLine("        Summaries=\"{Binding GridSummaries}\"");
            }

            if (UsesReadOnlyBinding(exampleId))
            {
                markup.AppendLine("        IsGridReadOnly=\"{Binding IsGridReadOnly}\"");
            }

            markup.AppendLine("        LanguageCode=\"{Binding LanguageCode}\"");
            markup.AppendLine("        />");
            markup.AppendLine("  </StackPanel>");
            markup.AppendLine(spec.MarkupFooter);
            return markup.ToString().TrimEnd();
        }

        private static IEnumerable<string> BuildToolbarMarkup(string exampleId)
        {
            if (exampleId == "selection")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Copy selection\" Click=\"HandleCopySelectionClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Select visible rows\" Click=\"HandleSelectVisibleRowsClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Clear selection\" Click=\"HandleClearSelectionClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "editing" || exampleId == "rich-editors")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Commit edits\" Click=\"HandleCommitEditsClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Cancel edits\" Click=\"HandleCancelEditsClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "filtering")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Focus Municipality filter\" Click=\"HandleFocusMunicipalityFilterClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Focus Owner filter\" Click=\"HandleFocusOwnerFilterClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Clear column filters\" Click=\"HandleClearColumnFiltersClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "summaries" || exampleId == "summary-designer")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <ComboBox Width=\"220\" ItemsSource=\"{Binding AvailableSummaryColumns}\" DisplayMemberPath=\"Header\" SelectedItem=\"{Binding SelectedSummaryColumn}\" />",
                    "  <ComboBox Width=\"180\" Margin=\"10,0,0,0\" ItemsSource=\"{Binding AvailableSummaryTypes}\" DisplayMemberPath=\"DisplayName\" SelectedItem=\"{Binding SelectedSummaryType}\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Add summary\" Click=\"HandleAddSummaryClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Reset summaries\" Click=\"HandleResetSummariesClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "column-layout")
            {
                return new[]
                {
                    "<TextBlock Margin=\"0,0,0,16\"",
                    "           Text=\"Use the grid options menu for column visibility and the column header popup for freeze, unfreeze and auto-fit actions.\"",
                    "           TextWrapping=\"Wrap\" />",
                };
            }

            if (exampleId == "state-persistence")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Save state\" Click=\"HandleSaveStateClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Restore state\" Click=\"HandleRestoreStateClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Reset state\" Click=\"HandleResetStateClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "remote-data")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Previous page\" Click=\"HandlePreviousRemotePageClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Next page\" Click=\"HandleNextRemotePageClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Refresh page\" Click=\"HandleRefreshRemoteClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "hierarchy")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Expand all nodes\" Click=\"HandleExpandHierarchyClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Collapse all nodes\" Click=\"HandleCollapseHierarchyClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "master-detail")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Expand all nodes\" Click=\"HandleExpandHierarchyClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Collapse all nodes\" Click=\"HandleCollapseHierarchyClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Show detail fields outside\" Click=\"HandleToggleMasterDetailPlacementClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "personalization")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <TextBox Width=\"220\" Text=\"{Binding GridSearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Apply search\" Click=\"HandleApplySearchClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Clear search\" Click=\"HandleClearSearchClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Toggle Owner column\" Click=\"HandleToggleOwnerClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Save view\" Click=\"HandleSaveViewClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Apply view\" Click=\"HandleApplyViewClick\" />",
                    "</WrapPanel>",
                };
            }

            if (exampleId == "export-import")
            {
                return new[]
                {
                    "<WrapPanel Margin=\"0,0,0,16\">",
                    "  <Button Content=\"Export CSV\" Click=\"HandleExportCsvClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Import sample CSV\" Click=\"HandleImportSampleCsvClick\" />",
                    "  <Button Margin=\"10,0,0,0\" Content=\"Restore GIS data\" Click=\"HandleRestoreSourceClick\" />",
                    "</WrapPanel>",
                    "<TextBlock Margin=\"0,0,0,10\" Text=\"{Binding TransferStatusText}\" />",
                    "<TextBox Height=\"140\" Text=\"{Binding TransferPreviewText, Mode=OneWay}\" IsReadOnly=\"True\" AcceptsReturn=\"True\" VerticalScrollBarVisibility=\"Auto\" />",
                };
            }

            return Array.Empty<string>();
        }

        private static string BuildViewModel(string exampleId)
        {
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            if (exampleId == "constraints")
            {
                builder.AppendLine("using System.Linq;");
            }
            if (exampleId == "remote-data")
            {
                builder.AppendLine("using System.Net.Http;");
                builder.AppendLine("using System.Threading.Tasks;");
            }

            builder.AppendLine("using PhialeGrid.Core.Columns;");
            builder.AppendLine("using PhialeGrid.Core.Data;");
            builder.AppendLine("using PhialeGrid.Core.Editing;");
            if (exampleId == "constraints")
            {
                builder.AppendLine("using PhialeGrid.Core.Validation;");
            }
            if (UsesGroupsBinding(exampleId) || UsesSortsBinding(exampleId))
            {
                builder.AppendLine("using PhialeGrid.Core.Query;");
            }

            if (UsesSummariesBinding(exampleId))
            {
                builder.AppendLine("using PhialeGrid.Core.Summaries;");
            }

            builder.AppendLine("using PhialeTech.Components.Shared.Core;");
            builder.AppendLine("using PhialeTech.Components.Shared.Model;");
            builder.AppendLine("using PhialeTech.Components.Shared.Services;");
            builder.AppendLine();
            builder.AppendLine("namespace Demo.Snippets");
            builder.AppendLine("{");
            builder.AppendLine("public sealed class " + GetClassName(exampleId) + (exampleId == "remote-data" ? " : BindableBase" : string.Empty));
            builder.AppendLine("{");

            if (exampleId == "remote-data")
            {
                builder.AppendLine("    private const int PageSize = 20;");
                builder.AppendLine("    private readonly IDemoRemoteGridClient _remoteClient =");
                builder.AppendLine("        new DemoRemoteGridHttpClient(new HttpClient(), \"http://127.0.0.1:5080/\");");
                builder.AppendLine("    private IReadOnlyList<DemoGisRecordViewModel> _gridRecords;");
                builder.AppendLine("    private IEditSessionContext _gridEditSessionContext;");
                builder.AppendLine("    private int _refreshVersion;");
                builder.AppendLine();
            }

            if (exampleId == "hierarchy")
            {
                builder.AppendLine("    private readonly DemoGisHierarchyDefinition _hierarchy = DemoGisHierarchyBuilder.Build(new DemoFeatureCatalog());");
                builder.AppendLine();
            }

            if (exampleId == "master-detail")
            {
                builder.AppendLine("    private readonly DemoGisHierarchyDefinition _hierarchy = DemoGisMasterDetailBuilder.Build(new DemoFeatureCatalog());");
                builder.AppendLine();
            }

            if (exampleId == "personalization")
            {
                builder.AppendLine("    public string GridSearchText { get; set; } = string.Empty;");
                builder.AppendLine("    public IReadOnlyList<string> SavedViewNames { get; } = new[] { \"Operations\", \"Review\" };");
                builder.AppendLine();
            }

            if (exampleId == "export-import")
            {
                builder.AppendLine("    public string TransferStatusText { get; private set; } = \"Export the current visible view or import the sample CSV dataset.\";");
                builder.AppendLine("    public string TransferPreviewText { get; private set; } = string.Empty;");
                builder.AppendLine();
            }

            builder.AppendLine("    public string LanguageCode { get; } = \"en\";");
            builder.AppendLine();
            if (exampleId == "master-detail")
            {
                AppendMasterDetailColumns(builder);
            }
            else if (exampleId == "constraints")
            {
                AppendConstraintFields(builder);
            }
            else
            {
                AppendColumns(builder, exampleId);
            }
            builder.AppendLine();

            if (exampleId == "remote-data")
            {
                builder.AppendLine("    public IReadOnlyList<DemoGisRecordViewModel> GridRecords");
                builder.AppendLine("    {");
                builder.AppendLine("        get => _gridRecords;");
                builder.AppendLine("        private set");
                builder.AppendLine("        {");
                builder.AppendLine("            if (SetProperty(ref _gridRecords, value))");
                builder.AppendLine("            {");
                builder.AppendLine("                GridEditSessionContext = BuildGridEditSessionContext(_gridRecords, GridColumns);");
                builder.AppendLine("            }");
                builder.AppendLine("        }");
                builder.AppendLine("    }");
                builder.AppendLine();
                builder.AppendLine("    public IEditSessionContext GridEditSessionContext");
                builder.AppendLine("    {");
                builder.AppendLine("        get => _gridEditSessionContext;");
                builder.AppendLine("        private set => SetProperty(ref _gridEditSessionContext, value);");
                builder.AppendLine("    }");
                builder.AppendLine();
                builder.AppendLine("    public " + GetClassName(exampleId) + "()");
                builder.AppendLine("    {");
                builder.AppendLine("        LoadPageAsync(0).GetAwaiter().GetResult();");
                builder.AppendLine("    }");
            }
            else
            {
                builder.AppendLine("    public IReadOnlyList<DemoGisRecordViewModel> GridRecords { get; }");
                builder.AppendLine();
                builder.AppendLine("    public IEditSessionContext GridEditSessionContext { get; }");
                builder.AppendLine();
                builder.AppendLine("    public " + GetClassName(exampleId) + "()");
                builder.AppendLine("    {");
                builder.AppendLine("        GridRecords = new DemoFeatureCatalog().GetGisRecords();");
                if (exampleId == "constraints")
                {
                    builder.AppendLine("        GridEditSessionContext = BuildGridEditSessionContext(GridRecords, GridFieldDefinitions);");
                }
                else
                {
                    builder.AppendLine("        GridEditSessionContext = BuildGridEditSessionContext(GridRecords, GridColumns);");
                }
                builder.AppendLine("    }");
            }

            if (UsesGroupsBinding(exampleId))
            {
                builder.AppendLine();
                builder.AppendLine("    public IReadOnlyList<GridGroupDescriptor> GridGroups { get; set; } =");
                builder.AppendLine("        new[] { new GridGroupDescriptor(\"Category\", GridSortDirection.Ascending) };");
            }

            if (UsesSortsBinding(exampleId))
            {
                builder.AppendLine();
                builder.AppendLine("    public IReadOnlyList<GridSortDescriptor> GridSorts { get; set; } =");
                builder.AppendLine("        new[]");
                builder.AppendLine("        {");
                builder.AppendLine("            new GridSortDescriptor(\"Category\", GridSortDirection.Ascending),");
                builder.AppendLine("            new GridSortDescriptor(\"LastInspection\", GridSortDirection.Descending),");
                builder.AppendLine("        };");
            }

            if (UsesSummariesBinding(exampleId))
            {
                builder.AppendLine();
                builder.AppendLine("    public IReadOnlyList<GridSummaryDescriptor> GridSummaries { get; } = new[]");
                builder.AppendLine("    {");
                builder.AppendLine("        new GridSummaryDescriptor(\"AreaSquareMeters\", GridSummaryType.Sum, typeof(decimal)),");
                builder.AppendLine("        new GridSummaryDescriptor(\"LengthMeters\", GridSummaryType.Sum, typeof(decimal)),");
                builder.AppendLine("        new GridSummaryDescriptor(\"ObjectId\", GridSummaryType.Count, typeof(string)),");
                builder.AppendLine("    };");

                if (exampleId == "summaries")
                {
                    builder.AppendLine();
                    builder.AppendLine("    public IReadOnlyList<object> AvailableSummaryColumns { get; } = new[]");
                    builder.AppendLine("    {");
                    builder.AppendLine("        new { Header = \"Area [m2]\", ColumnId = \"AreaSquareMeters\" },");
                    builder.AppendLine("        new { Header = \"Length [m]\", ColumnId = \"LengthMeters\" },");
                    builder.AppendLine("        new { Header = \"Object ID\", ColumnId = \"ObjectId\" },");
                    builder.AppendLine("    };");
                    builder.AppendLine();
                    builder.AppendLine("    public IReadOnlyList<object> AvailableSummaryTypes { get; } = new[]");
                    builder.AppendLine("    {");
                    builder.AppendLine("        new { DisplayName = \"Sum\", Type = GridSummaryType.Sum },");
                    builder.AppendLine("        new { DisplayName = \"Count\", Type = GridSummaryType.Count },");
                    builder.AppendLine("    };");
                }
            }

            if (UsesReadOnlyBinding(exampleId))
            {
                builder.AppendLine();
                builder.AppendLine("    public bool IsGridReadOnly { get; } = false;");
            }

            if (exampleId == "hierarchy" || exampleId == "master-detail")
            {
                builder.AppendLine();
                builder.AppendLine("    public IReadOnlyList<PhialeGrid.Core.Hierarchy.GridHierarchyNode<object>> GridHierarchyRoots => _hierarchy.Roots;");
                builder.AppendLine();
                builder.AppendLine("    public PhialeGrid.Core.Hierarchy.GridHierarchyController<object> GridHierarchyController => _hierarchy.Controller;");
            }

            if (exampleId == "remote-data")
            {
                builder.AppendLine();
                builder.AppendLine("    public IReadOnlyList<GridSortDescriptor> GridSorts { get; set; } = Array.Empty<GridSortDescriptor>();");
                builder.AppendLine();
                builder.AppendLine("    public async Task LoadPageAsync(int pageIndex, bool refresh = false)");
                builder.AppendLine("    {");
                builder.AppendLine("        if (refresh)");
                builder.AppendLine("        {");
                builder.AppendLine("            _refreshVersion++;");
                builder.AppendLine("        }");
                builder.AppendLine();
                builder.AppendLine("        var result = await _remoteClient.QueryAsync(");
                builder.AppendLine("            new DemoRemoteQueryRequest(");
                builder.AppendLine("                offset: Math.Max(0, pageIndex) * PageSize,");
                builder.AppendLine("                size: PageSize,");
                builder.AppendLine("                sorts: GridSorts,");
                builder.AppendLine("                filterGroup: GridFilterGroup.EmptyAnd(),");
                builder.AppendLine("                refreshGeneration: _refreshVersion));");
                builder.AppendLine();
                builder.AppendLine("        GridRecords = result.Items;");
                builder.AppendLine("    }");
            }

            builder.AppendLine();
            if (exampleId == "constraints")
            {
                builder.AppendLine("    private static IEditSessionContext BuildGridEditSessionContext(");
                builder.AppendLine("        IReadOnlyList<DemoGisRecordViewModel> records,");
                builder.AppendLine("        IReadOnlyList<IEditSessionFieldDefinition> fieldDefinitions)");
                builder.AppendLine("    {");
            }
            else
            {
                builder.AppendLine("    private static IEditSessionContext BuildGridEditSessionContext(");
                builder.AppendLine("        IReadOnlyList<DemoGisRecordViewModel> records,");
                builder.AppendLine("        IReadOnlyList<GridColumnDefinition> columns)");
                builder.AppendLine("    {");
                builder.AppendLine("        var fieldDefinitions = ObjectEditSessionFieldDefinitionFactory.CreateFromGridColumns(columns);");
            }
            builder.AppendLine("        var dataSource = new InMemoryEditSessionDataSource<DemoGisRecordViewModel>(");
            builder.AppendLine("            records ?? Array.Empty<DemoGisRecordViewModel>(),");
            builder.AppendLine("            fieldDefinitions);");
            builder.AppendLine("        return new EditSessionContext<DemoGisRecordViewModel>(dataSource, record => record.ObjectId);");
            builder.AppendLine("    }");

            builder.AppendLine("}");
            builder.AppendLine("}");
            return builder.ToString().TrimEnd();
        }

        private static void AppendColumns(StringBuilder builder, string exampleId)
        {
            builder.AppendLine("    public IReadOnlyList<GridColumnDefinition> GridColumns { get; } = new[]");
            builder.AppendLine("    {");
            foreach (var column in exampleId == "master-detail" ? MasterDetailColumns : Columns)
            {
                var isVisible = !(exampleId == "editing" && column.Id == "ObjectId") &&
                    !(column.Id == "ScaleHint" && exampleId != "editing");

                var line = new StringBuilder();
                line.Append("        new GridColumnDefinition(\"")
                    .Append(column.Id)
                    .Append("\", \"")
                    .Append(column.Header)
                    .Append("\", width: ")
                    .Append(column.Width.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                    .Append("d, displayIndex: ")
                    .Append(column.DisplayIndex)
                    .Append(", valueType: typeof(")
                    .Append(column.ValueTypeName)
                    .Append("), isEditable: ")
                    .Append(column.IsEditable ? "true" : "false")
                    .Append(", isVisible: ")
                    .Append(isVisible ? "true" : "false");

                if (!string.IsNullOrWhiteSpace(column.EditorKind))
                {
                    line.Append(", editorKind: ").Append(column.EditorKind);
                }

                if (!string.IsNullOrWhiteSpace(column.EditorItems))
                {
                    line.Append(", editorItems: ").Append(column.EditorItems);
                }

                if (!string.IsNullOrWhiteSpace(column.EditMask))
                {
                    line.Append(", editMask: ").Append(column.EditMask);
                }

                line.Append("),");
                builder.AppendLine(line.ToString());
            }

            builder.AppendLine("    };");
        }

        private static void AppendConstraintFields(StringBuilder builder)
        {
            builder.AppendLine("    public IReadOnlyList<IEditSessionFieldDefinition> GridFieldDefinitions { get; } = BuildGridFieldDefinitions();");
            builder.AppendLine();
            builder.AppendLine("    public IReadOnlyList<GridColumnDefinition> GridColumns { get; } =");
            builder.AppendLine("        ObjectEditSessionFieldDefinitionFactory.CreateGridColumns(GridFieldDefinitions);");
            builder.AppendLine();
            builder.AppendLine("    private static IReadOnlyList<IEditSessionFieldDefinition> BuildGridFieldDefinitions()");
            builder.AppendLine("    {");
            builder.AppendLine("        var statusOptions = new[] { \"Active\", \"Verified\", \"NeedsReview\", \"UnderMaintenance\", \"Planned\", \"Retired\" };");
            builder.AppendLine("        var priorityOptions = new[] { \"Critical\", \"High\", \"Medium\", \"Low\" };");
            builder.AppendLine("        var ownerOptions = new[] { \"City Infrastructure\", \"Field Team Alpha\", \"Field Team Beta\", \"Municipal Dispatch\", \"Utilities North\" };");
            builder.AppendLine();
            builder.AppendLine("        return new IEditSessionFieldDefinition[]");
            builder.AppendLine("        {");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"Category\",");
            builder.AppendLine("                \"Category\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.Category,");
            builder.AppendLine("                setter: (_, __) => { },");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.Category),");
            builder.AppendLine("                valueKind: \"Text\",");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"Category\", \"Category\", width: 150.0d, displayIndex: 0, valueType: typeof(string), isEditable: false, isVisible: true, valueKind: \"Text\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"ObjectName\",");
            builder.AppendLine("                \"Object name\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.ObjectName,");
            builder.AppendLine("                setter: (record, value) => record.ObjectName = value as string ?? string.Empty,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.ObjectName),");
            builder.AppendLine("                valueKind: \"Text\",");
            builder.AppendLine("                validationConstraints: new TextValidationConstraints(required: true, minLength: 3, maxLength: 120),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"ObjectName\", \"Object name\", width: 260.0d, displayIndex: 1, valueType: typeof(string), isEditable: true, isVisible: true, valueKind: \"Text\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"ObjectId\",");
            builder.AppendLine("                \"Object ID\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.ObjectId,");
            builder.AppendLine("                setter: (_, __) => { },");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.ObjectId),");
            builder.AppendLine("                valueKind: \"Code\",");
            builder.AppendLine("                validationConstraints: new TextValidationConstraints(required: true, minLength: 8, maxLength: 24, pattern: \"^[A-Z]{2,4}-[A-Z]{3}-[A-Z]{3}-[0-9]{4}$\"),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"ObjectId\", \"Object ID\", width: 180.0d, displayIndex: 2, valueType: typeof(string), isEditable: true, isVisible: true, valueKind: \"Code\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"GeometryType\",");
            builder.AppendLine("                \"Geometry type\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.GeometryType,");
            builder.AppendLine("                setter: (_, __) => { },");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.GeometryType),");
            builder.AppendLine("                valueKind: \"Text\",");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"GeometryType\", \"Geometry type\", width: 130.0d, displayIndex: 3, valueType: typeof(string), isEditable: false, isVisible: true, valueKind: \"Text\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"Status\",");
            builder.AppendLine("                \"Status\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.Status,");
            builder.AppendLine("                setter: (record, value) => record.Status = value as string ?? string.Empty,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.Status),");
            builder.AppendLine("                valueKind: \"Status\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.Combo,");
            builder.AppendLine("                editorItems: statusOptions,");
            builder.AppendLine("                validationConstraints: new LookupValidationConstraints(statusOptions.Cast<object>().ToArray(), required: true),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"Status\", \"Status\", width: 150.0d, displayIndex: 4, valueType: typeof(string), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.Combo, editorItems: statusOptions, valueKind: \"Status\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"Priority\",");
            builder.AppendLine("                \"Priority\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.Priority,");
            builder.AppendLine("                setter: (record, value) => record.Priority = value as string ?? string.Empty,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.Priority),");
            builder.AppendLine("                valueKind: \"Status\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.Combo,");
            builder.AppendLine("                editorItems: priorityOptions,");
            builder.AppendLine("                validationConstraints: new LookupValidationConstraints(priorityOptions.Cast<object>().ToArray(), required: true),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"Priority\", \"Priority\", width: 120.0d, displayIndex: 5, valueType: typeof(string), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.Combo, editorItems: priorityOptions, valueKind: \"Status\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"Visible\",");
            builder.AppendLine("                \"Visible\",");
            builder.AppendLine("                typeof(bool),");
            builder.AppendLine("                getter: record => record.Visible,");
            builder.AppendLine("                setter: (record, value) => record.Visible = value is bool boolValue && boolValue,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.Visible),");
            builder.AppendLine("                valueKind: \"Boolean\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.CheckBox,");
            builder.AppendLine("                validationConstraints: new BooleanValidationConstraints(required: true),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"Visible\", \"Visible\", width: 90.0d, displayIndex: 6, valueType: typeof(bool), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.CheckBox, valueKind: \"Boolean\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"EditableFlag\",");
            builder.AppendLine("                \"Editable\",");
            builder.AppendLine("                typeof(bool),");
            builder.AppendLine("                getter: record => record.EditableFlag,");
            builder.AppendLine("                setter: (record, value) => record.EditableFlag = value is bool boolValue && boolValue,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.EditableFlag),");
            builder.AppendLine("                valueKind: \"Boolean\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.CheckBox,");
            builder.AppendLine("                validationConstraints: new BooleanValidationConstraints(required: true),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"EditableFlag\", \"Editable\", width: 96.0d, displayIndex: 7, valueType: typeof(bool), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.CheckBox, valueKind: \"Boolean\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"LastInspection\",");
            builder.AppendLine("                \"Last inspection\",");
            builder.AppendLine("                typeof(DateTime),");
            builder.AppendLine("                getter: record => record.LastInspection,");
            builder.AppendLine("                setter: (record, value) => record.LastInspection = value is DateTime dateTimeValue ? dateTimeValue : default,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.LastInspection),");
            builder.AppendLine("                valueKind: \"DateTime\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.DatePicker,");
            builder.AppendLine("                validationConstraints: new DateValidationConstraints(required: true, minDate: new DateTime(2020, 1, 1), maxDate: new DateTime(2035, 12, 31)),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"LastInspection\", \"Last inspection\", width: 150.0d, displayIndex: 8, valueType: typeof(DateTime), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.DatePicker, valueKind: \"DateTime\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"UpdatedAt\",");
            builder.AppendLine("                \"Updated at\",");
            builder.AppendLine("                typeof(DateTime),");
            builder.AppendLine("                getter: record => record.UpdatedAt,");
            builder.AppendLine("                setter: (_, __) => { },");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.UpdatedAt),");
            builder.AppendLine("                valueKind: \"DateTime\",");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"UpdatedAt\", \"Updated at\", width: 170.0d, displayIndex: 9, valueType: typeof(DateTime), isEditable: false, isVisible: true, valueKind: \"DateTime\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"Owner\",");
            builder.AppendLine("                \"Owner\",");
            builder.AppendLine("                typeof(string),");
            builder.AppendLine("                getter: record => record.Owner,");
            builder.AppendLine("                setter: (record, value) => record.Owner = value as string ?? string.Empty,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.Owner),");
            builder.AppendLine("                valueKind: \"Text\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.Autocomplete,");
            builder.AppendLine("                editorItems: ownerOptions,");
            builder.AppendLine("                validationConstraints: new TextValidationConstraints(required: true, minLength: 3, maxLength: 120, allowedValues: ownerOptions),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"Owner\", \"Owner\", width: 180.0d, displayIndex: 10, valueType: typeof(string), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.Autocomplete, editorItems: ownerOptions, valueKind: \"Text\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"MaintenanceBudget\",");
            builder.AppendLine("                \"Budget [PLN]\",");
            builder.AppendLine("                typeof(decimal),");
            builder.AppendLine("                getter: record => record.MaintenanceBudget,");
            builder.AppendLine("                setter: (_, __) => { },");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.MaintenanceBudget),");
            builder.AppendLine("                valueKind: \"Currency\",");
            builder.AppendLine("                validationConstraints: new DecimalValidationConstraints(required: true, minValue: 0m, maxValue: 999999.99m, scale: 2, precision: 8, allowNegative: false),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"MaintenanceBudget\", \"Budget [PLN]\", width: 150.0d, displayIndex: 11, valueType: typeof(decimal), isEditable: true, isVisible: true, valueKind: \"Currency\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"CompletionPercent\",");
            builder.AppendLine("                \"Completion [%]\",");
            builder.AppendLine("                typeof(decimal),");
            builder.AppendLine("                getter: record => record.CompletionPercent,");
            builder.AppendLine("                setter: (_, __) => { },");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.CompletionPercent),");
            builder.AppendLine("                valueKind: \"Percent\",");
            builder.AppendLine("                validationConstraints: new DecimalValidationConstraints(required: true, minValue: 0m, maxValue: 100m, scale: 1, precision: 4, allowNegative: false),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"CompletionPercent\", \"Completion [%]\", width: 140.0d, displayIndex: 12, valueType: typeof(decimal), isEditable: true, isVisible: true, valueKind: \"Percent\")),");
            builder.AppendLine("            new EditSessionFieldDefinition<DemoGisRecordViewModel>(");
            builder.AppendLine("                \"ScaleHint\",");
            builder.AppendLine("                \"Scale hint\",");
            builder.AppendLine("                typeof(int),");
            builder.AppendLine("                getter: record => record.ScaleHint,");
            builder.AppendLine("                setter: (record, value) => record.ScaleHint = value is int intValue ? intValue : 0,");
            builder.AppendLine("                fieldPath: nameof(DemoGisRecordViewModel.ScaleHint),");
            builder.AppendLine("                valueKind: \"Number\",");
            builder.AppendLine("                editorKind: GridColumnEditorKind.MaskedText,");
            builder.AppendLine("                editMask: \"^[0-9]{0,6}$\",");
            builder.AppendLine("                validationConstraints: new IntegerValidationConstraints(required: true, minValue: 100, maxValue: 100000, allowZero: false, allowNegative: false),");
            builder.AppendLine("                gridColumnDefinition: new GridColumnDefinition(\"ScaleHint\", \"Scale hint\", width: 120.0d, displayIndex: 13, valueType: typeof(int), isEditable: true, isVisible: true, editorKind: GridColumnEditorKind.MaskedText, editMask: \"^[0-9]{0,6}$\", valueKind: \"Number\")),");
            builder.AppendLine("        };");
            builder.AppendLine("    }");
        }

        private static void AppendMasterDetailColumns(StringBuilder builder)
        {
            builder.AppendLine("    // The main grid keeps the master fields visible.");
            AppendNamedColumns(builder, "MasterColumns", MasterDetailColumns.Take(2));
            builder.AppendLine();
            builder.AppendLine("    // The expanded detail overlay renders these detail-only columns.");
            AppendNamedColumns(builder, "DetailColumns", MasterDetailColumns.Skip(2));
            builder.AppendLine();
            AppendNamedColumns(builder, "GridColumns", MasterDetailColumns);
            builder.AppendLine();
            builder.AppendLine("    public IReadOnlyList<string> DetailColumnIds { get; } = new[] { \"ObjectName\", \"ObjectId\", \"GeometryType\", \"Status\" };");
            builder.AppendLine();
            builder.AppendLine("    public string MasterDisplayColumnId { get; } = \"Category\";");
            builder.AppendLine();
            builder.AppendLine("    public string DetailDisplayColumnId { get; } = \"ObjectName\";");
        }

        private static void AppendNamedColumns(StringBuilder builder, string propertyName, IEnumerable<ColumnSpec> columns)
        {
            builder.AppendLine("    public IReadOnlyList<GridColumnDefinition> " + propertyName + " { get; } = new[]");
            builder.AppendLine("    {");

            foreach (var column in columns)
            {
                builder.AppendLine(BuildColumnLine(column, isVisible: true));
            }

            builder.AppendLine("    };");
        }

        private static string BuildColumnLine(ColumnSpec column, bool isVisible)
        {
            var line = new StringBuilder();
            line.Append("        new GridColumnDefinition(\"")
                .Append(column.Id)
                .Append("\", \"")
                .Append(column.Header)
                .Append("\", width: ")
                .Append(column.Width.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                .Append("d, displayIndex: ")
                .Append(column.DisplayIndex)
                .Append(", valueType: typeof(")
                .Append(column.ValueTypeName)
                .Append("), isEditable: ")
                .Append(column.IsEditable ? "true" : "false")
                .Append(", isVisible: ")
                .Append(isVisible ? "true" : "false");

            if (!string.IsNullOrWhiteSpace(column.EditorKind))
            {
                line.Append(", editorKind: ").Append(column.EditorKind);
            }

            if (!string.IsNullOrWhiteSpace(column.EditorItems))
            {
                line.Append(", editorItems: ").Append(column.EditorItems);
            }

            if (!string.IsNullOrWhiteSpace(column.EditMask))
            {
                line.Append(", editMask: ").Append(column.EditMask);
            }

            line.Append("),");
            return line.ToString();
        }

        private static string BuildHostCode(string exampleId)
        {
            if (exampleId == "selection")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private void HandleCopySelectionClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.CopySelectionToClipboard();",
                    "}",
                    string.Empty,
                    "private void HandleSelectVisibleRowsClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.SelectVisibleRows();",
                    "}",
                    string.Empty,
                    "private void HandleClearSelectionClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.ClearSelection();",
                    "}",
                });
            }

            if (exampleId == "editing" || exampleId == "rich-editors")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private void HandleCommitEditsClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.CommitEdits();",
                    "}",
                    string.Empty,
                    "private void HandleCancelEditsClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.CancelEdits();",
                    "}",
                });
            }

            if (exampleId == "filtering")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private void HandleFocusMunicipalityFilterClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.FocusColumnFilter(\"Municipality\");",
                    "}",
                    string.Empty,
                    "private void HandleFocusOwnerFilterClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.FocusColumnFilter(\"Owner\");",
                    "}",
                    string.Empty,
                    "private void HandleClearColumnFiltersClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.ClearFilters();",
                    "}",
                });
            }

            if (exampleId == "summaries" || exampleId == "summary-designer")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private void HandleAddSummaryClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    ViewModel.AddSelectedSummary();",
                    "}",
                    string.Empty,
                    "private void HandleResetSummariesClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    ViewModel.ResetSummaries();",
                    "}",
                    string.Empty,
                    "private void HandleRemoveSummaryClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    if (!(sender is FrameworkElement frameworkElement) || !(frameworkElement.Tag is DemoConfiguredSummaryViewModel summary))",
                    "    {",
                    "        return;",
                    "    }",
                    "",
                    "    ViewModel.RemoveSummary(summary.ColumnId, summary.Type);",
                    "}",
                });
            }

            if (exampleId == "column-layout")
            {
                return string.Empty;
            }

            if (exampleId == "state-persistence")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private const string GridStateKey = \"Demo/Grid/StatePersistence\";",
                    "private ApplicationStateManager _applicationStateManager;",
                    "private PhialeTech.PhialeGrid.Wpf.State.PhialeGridViewStateComponent _gridStateComponent;",
                    "private ApplicationStateRegistration _gridStateRegistration;",
                    string.Empty,
                    "private void ConfigureStatePersistence()",
                    "{",
                    "    _applicationStateManager = App.ApplicationStateManager;",
                    "    _gridStateComponent = new PhialeTech.PhialeGrid.Wpf.State.PhialeGridViewStateComponent(DemoGrid);",
                    "    _gridStateRegistration = _applicationStateManager.Register(GridStateKey, _gridStateComponent);",
                    "}",
                    string.Empty,
                    "private void HandleSaveStateClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    _applicationStateManager.SaveRegisteredState(GridStateKey);",
                    "}",
                    string.Empty,
                    "private void HandleRestoreStateClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    _applicationStateManager.TryRestoreRegisteredState(GridStateKey);",
                    "}",
                    string.Empty,
                    "private void HandleResetStateClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    _applicationStateManager.Delete(GridStateKey);",
                    "    DemoGrid.ResetState();",
                    "}",
                });
            }

            if (exampleId == "remote-data")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private async void HandlePreviousRemotePageClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    await ViewModel.LoadPreviousRemotePageAsync();",
                    "}",
                    string.Empty,
                    "private async void HandleNextRemotePageClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    await ViewModel.LoadNextRemotePageAsync();",
                    "}",
                    string.Empty,
                    "private async void HandleRefreshRemoteClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    await ViewModel.RefreshRemotePageAsync();",
                    "}",
                });
            }

            if (exampleId == "hierarchy")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private async void HandleExpandHierarchyClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    await DemoGrid.ExpandAllHierarchyAsync();",
                    "}",
                    string.Empty,
                    "private void HandleCollapseHierarchyClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.CollapseAllHierarchy();",
                    "}",
                    string.Empty,
                    "private void ConfigureHierarchy()",
                    "{",
                    "    DemoGrid.SetHierarchySource(ViewModel.GridHierarchyRoots, ViewModel.GridHierarchyController);",
                    "}",
                });
            }

            if (exampleId == "master-detail")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private bool _showDetailFieldsOutside;",
                    string.Empty,
                    "private async void HandleExpandHierarchyClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    await DemoGrid.ExpandAllHierarchyAsync();",
                    "}",
                    string.Empty,
                    "private void HandleCollapseHierarchyClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.CollapseAllHierarchy();",
                    "}",
                    string.Empty,
                    "private void HandleToggleMasterDetailPlacementClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    _showDetailFieldsOutside = !_showDetailFieldsOutside;",
                    "    ConfigureMasterDetail();",
                    "}",
                    string.Empty,
                    "private void ConfigureMasterDetail()",
                    "{",
                    "    DemoGrid.SetMasterDetailSource(",
                    "        ViewModel.GridHierarchyRoots,",
                    "        ViewModel.GridHierarchyController,",
                    "        ViewModel.DetailColumnIds,",
                    "        masterDisplayColumnId: ViewModel.MasterDisplayColumnId,",
                    "        detailDisplayColumnId: ViewModel.DetailDisplayColumnId,",
                    "        detailHeaderPlacementMode: _showDetailFieldsOutside",
                    "            ? GridMasterDetailHeaderPlacementMode.Outside",
                    "            : GridMasterDetailHeaderPlacementMode.Inside);",
                    "}",
                });
            }

            if (exampleId == "personalization")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private readonly GridNamedViewCatalog _namedViews = new GridNamedViewCatalog();",
                    string.Empty,
                    "private void HandleApplySearchClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.ApplyGlobalSearch(ViewModel.GridSearchText);",
                    "}",
                    string.Empty,
                    "private void HandleClearSearchClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    ViewModel.GridSearchText = string.Empty;",
                    "    DemoGrid.ClearGlobalSearch();",
                    "}",
                    string.Empty,
                    "private void HandleToggleOwnerClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.SetColumnVisibility(\"Owner\", false);",
                    "}",
                    string.Empty,
                    "private void HandleSaveViewClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    if (DemoGrid == null || string.IsNullOrWhiteSpace(ViewModel.PendingViewName))",
                    "    {",
                    "        return;",
                    "    }",
                    "",
                    "    _namedViews.Save(new GridNamedViewDefinition(",
                    "        ViewModel.PendingViewName.Trim(),",
                    "        DemoGrid.SaveState()));",
                    "",
                    "    ViewModel.SetSavedViewNames(_namedViews.Names);",
                    "    ViewModel.SelectedSavedViewName = ViewModel.PendingViewName.Trim();",
                    "    ViewModel.PendingViewName = string.Empty;",
                    "}",
                    string.Empty,
                    "private void HandleApplyViewClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    if (DemoGrid == null || string.IsNullOrWhiteSpace(ViewModel.SelectedSavedViewName))",
                    "    {",
                    "        return;",
                    "    }",
                    "",
                    "    if (!_namedViews.TryGet(ViewModel.SelectedSavedViewName, out var namedView))",
                    "    {",
                    "        return;",
                    "    }",
                    "",
                    "    if (!string.IsNullOrWhiteSpace(namedView.GridState))",
                    "    {",
                    "        DemoGrid.LoadState(namedView.GridState);",
                    "    }",
                    "",
                    "    ViewModel.GridSearchText = DemoGrid.GlobalSearchText;",
                    "}",
                });
            }

            if (exampleId == "export-import")
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "private void HandleExportCsvClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    var csv = DemoGrid.ExportCurrentViewToCsv();",
                    "    var exportedRowCount = DemoGrid.RowsView.Cast<object>()",
                    "        .OfType<GridDisplayRowModel>()",
                    "        .Count(row => row.SourceRow != null);",
                    "    ViewModel.MarkTransferExported(exportedRowCount, DemoGrid.VisibleColumns.Count, csv);",
                    "}",
                    string.Empty,
                    "private void HandleImportSampleCsvClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    var sampleCsv = ViewModel.BuildSampleImportCsv();",
                    "    var importedRecords = DemoGisCsvTransferService.Import(sampleCsv, ViewModel.GridColumns);",
                    "    DemoGrid.ClearGlobalSearch();",
                    "    DemoGrid.ResetState();",
                    "    ViewModel.GridSearchText = DemoGrid.GlobalSearchText;",
                    "    ViewModel.ReplaceGridRecords(importedRecords);",
                    "    ViewModel.MarkTransferImported(importedRecords.Count, sampleCsv);",
                    "}",
                    string.Empty,
                    "private void HandleRestoreSourceClick(object sender, RoutedEventArgs e)",
                    "{",
                    "    DemoGrid.ClearGlobalSearch();",
                    "    DemoGrid.ResetState();",
                    "    ViewModel.GridSearchText = DemoGrid.GlobalSearchText;",
                    "    ViewModel.RestoreDefaultGridRecords();",
                    "    ViewModel.MarkTransferRestored(ViewModel.GridRecords.Count);",
                    "}",
                });
            }

            return string.Empty;
        }

        private static string BuildApplicationStateAppHostFile()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using System.Windows;",
                "using PhialeTech.ComponentHost.State;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public partial class App : Application",
                "    {",
                "        public static ApplicationStateManager ApplicationStateManager { get; private set; }",
                string.Empty,
                "        protected override void OnStartup(StartupEventArgs e)",
                "        {",
                "            base.OnStartup(e);",
                string.Empty,
                "            var store = new JsonApplicationStateStore(\"PhialeTech.Components\");",
                "            ApplicationStateManager = new ApplicationStateManager(store);",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildDefinitionManagerViewModel()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.ComponentHost.Definitions;",
                "using PhialeTech.Components.Shared.Model;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public sealed class DefinitionManagerExampleViewModel",
                "    {",
                "        public DefinitionManagerExampleViewModel(DefinitionManager definitionManager)",
                "        {",
                "            var pageDefinition = definitionManager.Resolve<DemoComponentDefinition>(\"demo.definition-manager\");",
                "            var groupingDefinition = definitionManager.Resolve<DemoComponentDefinition>(\"demo.grid.grouping\");",
                string.Empty,
                "            PageDefinitionTitle = pageDefinition.Definition.TitleKey;",
                "            PageDefinitionSummary = pageDefinition.Definition.SummaryKey;",
                "            GroupingDefinitionKey = groupingDefinition.DefinitionKey;",
                "            GroupingDefinitionSourceId = groupingDefinition.SourceId;",
                "            GroupingDefinitionTitle = groupingDefinition.Definition.TitleKey;",
                "            GroupingDefinitionSummary = groupingDefinition.Definition.SummaryKey;",
                "        }",
                string.Empty,
                "        public string PageDefinitionTitle { get; }",
                string.Empty,
                "        public string PageDefinitionSummary { get; }",
                string.Empty,
                "        public string GroupingDefinitionKey { get; }",
                string.Empty,
                "        public string GroupingDefinitionSourceId { get; }",
                string.Empty,
                "        public string GroupingDefinitionTitle { get; }",
                string.Empty,
                "        public string GroupingDefinitionSummary { get; }",
                "    }",
                "}",
            });
        }

        private static string BuildDefinitionManagerDefinitionFile()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using PhialeTech.ComponentHost.Definitions;",
                "using PhialeTech.Components.Shared.Localization;",
                "using PhialeTech.Components.Shared.Model;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public static class DemoDefinitions",
                "    {",
                "        public static InMemoryDefinitionSource CreateSource()",
                "        {",
                "            return new InMemoryDefinitionSource(\"demo-local\")",
                "                .Add(\"demo.definition-manager\", new DemoComponentDefinition(",
                "                    definitionKind: \"page\",",
                "                    componentId: \"definition-manager\",",
                "                    titleKey: DemoTextKeys.ExampleDefinitionManagerTitle,",
                "                    summaryKey: DemoTextKeys.ExampleDefinitionManagerDescription,",
                "                    consumerHintKey: DemoTextKeys.DefinitionManagerDefinitionConsumerHint,",
                "                    stateOverlayHintKey: DemoTextKeys.DefinitionManagerDefinitionStateBoundary))",
                "                .Add(\"demo.grid.grouping\", new DemoComponentDefinition(",
                "                    definitionKind: \"screen\",",
                "                    componentId: \"grid\",",
                "                    titleKey: DemoTextKeys.ExampleGroupingTitle,",
                "                    summaryKey: DemoTextKeys.ExampleGroupingDescription,",
                "                    consumerHintKey: DemoTextKeys.DefinitionManagerGroupingConsumerHint,",
                "                    stateOverlayHintKey: DemoTextKeys.DefinitionManagerGroupingStateBoundary));",
                "        }",
                "    }",
                "}",
            });
        }

        private static string BuildDefinitionManagerAppHostFile()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "using System.Windows;",
                "using PhialeTech.ComponentHost.Definitions;",
                "using PhialeTech.ComponentHost.State;",
                string.Empty,
                "namespace Demo.Snippets",
                "{",
                "    public partial class App : Application",
                "    {",
                "        public static DefinitionManager DefinitionManager { get; private set; }",
                string.Empty,
                "        public static ApplicationStateManager ApplicationStateManager { get; private set; }",
                string.Empty,
                "        protected override void OnStartup(StartupEventArgs e)",
                "        {",
                "            base.OnStartup(e);",
                string.Empty,
                "            var definitionSource = DemoDefinitions.CreateSource();",
                "            DefinitionManager = new DefinitionManager(new[] { definitionSource });",
                "            ApplicationStateManager = new ApplicationStateManager(new JsonApplicationStateStore(\"PhialeTech.Components\"));",
                "        }",
                "    }",
                "}",
            });
        }

        private static bool UsesGroupsBinding(string exampleId)
        {
            return exampleId == "grouping" || exampleId == "state-persistence";
        }

        private static bool UsesSortsBinding(string exampleId)
        {
            return exampleId == "sorting" || exampleId == "state-persistence" || exampleId == "remote-data";
        }

        private static bool UsesSummariesBinding(string exampleId)
        {
            return exampleId == "summaries" || exampleId == "summary-designer" || exampleId == "state-persistence";
        }

        private static bool UsesReadOnlyBinding(string exampleId)
        {
            return exampleId == "editing" || exampleId == "rich-editors";
        }

        private static string GetClassName(string exampleId)
        {
            var parts = (exampleId ?? string.Empty)
                .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1));
            return string.Concat(parts) + "ExampleViewModel";
        }

        private static void AppendIndentedBlock(StringBuilder builder, string block, int indentWidth)
        {
            var indent = new string(' ', indentWidth);
            var lines = (block ?? string.Empty)
                .Replace("\r\n", "\n")
                .Split('\n');

            foreach (var line in lines)
            {
                builder.AppendLine(line.Length == 0 ? string.Empty : indent + line);
            }
        }

        private sealed class PlatformSpec
        {
            public string XamlExtension { get; private set; }

            public string GridTag { get; private set; }

            public string MarkupHeader { get; private set; }

            public string MarkupFooter { get; private set; }

            public string HostCodeFileName { get; private set; }

            public string HostBaseType { get; private set; }

            public bool WrapHostInClass { get; private set; }

            public static PlatformSpec Create(string platformKey)
            {
                var normalized = string.IsNullOrWhiteSpace(platformKey) ? "Wpf" : platformKey.Trim();
                if (string.Equals(normalized, "Avalonia", StringComparison.OrdinalIgnoreCase))
                {
                    return new PlatformSpec
                    {
                        XamlExtension = ".axaml",
                        GridTag = "grid:PhialeGrid",
                        HostCodeFileName = "ExampleHost.axaml.cs",
                        HostBaseType = "UserControl",
                        MarkupHeader =
                            "<UserControl xmlns=\"https://github.com/avaloniaui\"" + Environment.NewLine +
                            "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"" + Environment.NewLine +
                            "             xmlns:grid=\"clr-namespace:PhialeTech.PhialeGrid.Avalonia.Controls;assembly=PhialeTech.Grid.Avalonia\">",
                        MarkupFooter = "</UserControl>",
                    };
                }

                if (string.Equals(normalized, "WinUI", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalized, "WinUi", StringComparison.OrdinalIgnoreCase))
                {
                    return new PlatformSpec
                    {
                        XamlExtension = ".xaml",
                        GridTag = "grid:PhialeGrid",
                        HostCodeFileName = "ExampleHost.xaml.cs",
                        HostBaseType = "UserControl",
                        MarkupHeader =
                            "<UserControl" + Environment.NewLine +
                            "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" + Environment.NewLine +
                            "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"" + Environment.NewLine +
                            "    xmlns:grid=\"using:PhialeTech.PhialeGrid.WinUI.Controls\">",
                        MarkupFooter = "</UserControl>",
                    };
                }

                return new PlatformSpec
                {
                    XamlExtension = ".xaml",
                    GridTag = "grid:PhialeGrid",
                    HostCodeFileName = "ExampleHost.xaml.cs",
                    HostBaseType = "UserControl",
                    WrapHostInClass = true,
                    MarkupHeader =
                        "<UserControl" + Environment.NewLine +
                        "    x:Class=\"Demo.Snippets.ExampleHost\"" + Environment.NewLine +
                        "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" + Environment.NewLine +
                        "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"" + Environment.NewLine +
                        "    xmlns:grid=\"clr-namespace:PhialeTech.PhialeGrid.Wpf.Controls;assembly=PhialeTech.Grid.Wpf\">",
                    MarkupFooter = "</UserControl>",
                };
            }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(
                string id,
                string header,
                string valueTypeName,
                double width,
                int displayIndex,
                bool isEditable,
                string editorKind = null,
                string editorItems = null,
                string editMask = null)
            {
                Id = id;
                Header = header;
                ValueTypeName = valueTypeName;
                Width = width;
                DisplayIndex = displayIndex;
                IsEditable = isEditable;
                EditorKind = editorKind ?? string.Empty;
                EditorItems = editorItems ?? string.Empty;
                EditMask = editMask ?? string.Empty;
            }

            public string Id { get; }

            public string Header { get; }

            public string ValueTypeName { get; }

            public double Width { get; }

            public int DisplayIndex { get; }

            public bool IsEditable { get; }

            public string EditorKind { get; }

            public string EditorItems { get; }

            public string EditMask { get; }
        }
    }
}
