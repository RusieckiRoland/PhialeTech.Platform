using System;
using System.Windows;
using System.Windows.Controls;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.ComponentHost.Wpf.Hosting;
using PhialeTech.Yaml.Library;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Infrastructure.Loading;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Runtime.Services;
using PhialeTech.YamlApp.Wpf.Document;

namespace PhialeTech.Components.Wpf.Hosting
{
    public sealed class DemoYamlHostedSurfaceFactory : IWpfHostedSurfaceFactory
    {
        public bool CanCreate(IHostedSurfaceRequest request)
        {
            return request != null &&
                request.SurfaceKind == HostedSurfaceKind.YamlDocument &&
                (string.Equals(request.ContentKey, "demo.yaml.hosted-modal", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.ContentKey, "demo.yaml.generated-preview", StringComparison.OrdinalIgnoreCase));
        }

        public FrameworkElement CreateContent(IHostedSurfaceRequest request, IHostedSurfaceManager manager)
        {
            var documentState = BuildRuntimeState(request);
            var host = new YamlDocumentHost
            {
                RuntimeDocumentState = documentState,
            };
            host.ActionInvoked += (sender, args) => HandleYamlDocumentActionInvoked(args, manager);

            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = host,
            };
        }

        private static RuntimeDocumentState BuildRuntimeState(IHostedSurfaceRequest request)
        {
            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(
                ResolveYamlSource(request),
                new[] { typeof(YamlLibraryMarker).Assembly },
                "en");

            if (!compiled.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, compiled.Diagnostics));
            }

            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            if (!normalized.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, normalized.Diagnostics));
            }

            var resolvedForm = normalized.ResolvedDocument as ResolvedFormDocumentDefinition;
            if (resolvedForm == null)
            {
                throw new InvalidOperationException("The hosted modal demo requires a resolved form document.");
            }

            return new RuntimeDocumentStateFactory().Create(resolvedForm);
        }

        private static void HandleYamlDocumentActionInvoked(YamlDocumentActionInvokedEventArgs args, IHostedSurfaceManager manager)
        {
            if (args?.DocumentState == null || args.ActionState?.Action == null)
            {
                return;
            }

            var payload = new RuntimeDocumentJsonMapper().ToJson(args.DocumentState);
            if (args.ActionState.Action.ActionKind == DocumentActionKind.Cancel)
            {
                manager.TryCancelCurrent(args.ActionState.Id, payload);
                return;
            }

            manager.TryConfirmCurrent(args.ActionState.Id, payload);
        }

        private static string ResolveYamlSource(IHostedSurfaceRequest request)
        {
            if (request != null &&
                string.Equals(request.ContentKey, "demo.yaml.generated-preview", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(request.Payload))
            {
                return request.Payload;
            }

            return BuildSampleYaml(request);
        }

        private static string BuildSampleYaml(IHostedSurfaceRequest request)
        {
            var title = string.IsNullOrWhiteSpace(request.Title) ? "Review request" : request.Title.Replace(":", "-");
            var compact = request.PresentationMode == HostedPresentationMode.CompactModal;
            var sizeHint = compact ? "ModalCompact" : "ModalSheet";

            return string.Join(Environment.NewLine, new[]
            {
                "namespace: application.forms",
                "imports:",
                "  - domain.person",
                "  - application.forms.actionShells",
                "",
                "document:",
                "  id: hosted-modal-review-request",
                "  kind: Form",
                "  extends: review-sticky-header-footer",
                "  name: " + title,
                "  topRegionChrome: Merged",
                "  bottomRegionChrome: Merged",
                "  header:",
                "    title: Review request",
                "    subtitle: Customer verification",
                "    description: Modal YamlApp content hosted by the reusable ComponentHost pipeline.",
                "    status: Draft",
                "    context: " + sizeHint,
                "  footer:",
                "    note: Fields marked with * are required.",
                "    status: Draft saved locally",
                "    source: Demo hosted modal",
                "  fields:",
                "    - id: firstName",
                "      extends: firstName",
                "    - id: lastName",
                "      extends: lastName",
                "    - id: notes",
                "      extends: notes",
                "  layout:",
                "    type: Column",
                "    items:",
                "      - type: Container",
                "        caption: Reviewer",
                "        showBorder: true",
                "        variant: Compact",
                "        items:",
                "          - type: Row",
                "            items:",
                "              - fieldRef: firstName",
                "              - fieldRef: lastName",
                "      - type: Container",
                "        caption: Review notes",
                "        showBorder: true",
                "        items:",
                "          - fieldRef: notes",
            });
        }
    }
}
