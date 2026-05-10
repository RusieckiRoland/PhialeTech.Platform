using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using NUnit.Framework;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.ComponentHost.Presentation;
using PhialeTech.ComponentHost.Wpf.Hosting;
using PhialeTech.ComponentHost.Wpf.Controls;
using PhialeTech.ComponentHost.Wpf.Services;
using PhialeTech.Components.Wpf.Hosting;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Wpf.Document;
using UniversalInput.Contracts;

namespace PhialeTech.WebHost.Wpf.Tests
{
    [Apartment(ApartmentState.STA)]
    public sealed class YamlHostedModalLayoutTests
    {
        [Test]
        public void HostedYamlModal_WithAutoLayoutHeight_DoesNotMeasureToFullViewportHeight()
        {
            var request = new HostedSurfaceRequest
            {
                SurfaceKind = HostedSurfaceKind.YamlDocument,
                ContentKey = "demo.yaml.hosted-modal",
                PresentationMode = HostedPresentationMode.CompactModal,
                Size = HostedPresentationSize.Medium,
                Placement = HostedSheetPlacement.Center,
                Title = "Review request"
            };

            var content = new DemoYamlHostedSurfaceFactory().CreateContent(request, new NullHostedSurfaceManager());
            var host = (YamlDocumentHost)content;

            Assert.That(host.RuntimeDocumentState.Document.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Auto));
            Assert.That(host.VerticalAlignment, Is.EqualTo(VerticalAlignment.Top));

            content.Measure(new Size(844d, 920d));

            Assert.That(content.DesiredSize.Height, Is.LessThan(760d));
        }

        [Test]
        public void GeneratedPreviewYamlModal_WithAutoLayoutHeight_DoesNotMeasureToFullViewportHeight()
        {
            var request = new HostedSurfaceRequest
            {
                SurfaceKind = HostedSurfaceKind.YamlDocument,
                ContentKey = "demo.yaml.generated-preview",
                PresentationMode = HostedPresentationMode.CompactModal,
                Size = HostedPresentationSize.Large,
                Placement = HostedSheetPlacement.Center,
                Title = "Generated Yaml form",
                Payload = BuildGeneratedPreviewPayload()
            };

            var content = new DemoYamlHostedSurfaceFactory().CreateContent(request, new NullHostedSurfaceManager());
            var host = (YamlDocumentHost)content;

            Assert.That(host.RuntimeDocumentState.Document.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Auto));
            Assert.That(host.VerticalAlignment, Is.EqualTo(VerticalAlignment.Top));

            content.Measure(new Size(844d, 920d));

            Assert.That(content.DesiredSize.Height, Is.LessThan(760d));
        }

        [Test]
        public void CompactModalChrome_WithAutoHeightYamlContent_DoesNotStretchDialogToViewportHeight()
        {
            EnsureApplicationResources();

            var request = new HostedSurfaceRequest
            {
                SurfaceKind = HostedSurfaceKind.YamlDocument,
                ContentKey = "demo.yaml.generated-preview",
                PresentationMode = HostedPresentationMode.CompactModal,
                Size = HostedPresentationSize.Large,
                Placement = HostedSheetPlacement.Center,
                Title = "Generated Yaml form",
                Payload = BuildGeneratedPreviewPayload()
            };
            var content = new DemoYamlHostedSurfaceFactory().CreateContent(request, new NullHostedSurfaceManager());

            var dialogRoot = new Border
            {
                Margin = new Thickness(24d),
                Width = 900d,
                MaxWidth = 1120d,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var surfaceHost = new Grid();
            surfaceHost.Children.Add(new PhialeModalChromeSkiaPresenter());
            surfaceHost.Children.Add(new Border
            {
                Margin = new Thickness(8d),
                Child = new ContentPresenter { Content = content }
            });
            dialogRoot.Child = surfaceHost;

            var root = new Grid
            {
                Children =
                {
                    dialogRoot
                }
            };

            root.Measure(new Size(1000d, 980d));
            root.Arrange(new Rect(0d, 0d, 1000d, 980d));
            root.UpdateLayout();

            Assert.That(dialogRoot.ActualHeight, Is.LessThan(820d));
        }

        [Test]
        public void RealCompactModalTemplate_WithAutoHeightYamlContent_DoesNotStretchDialogToViewportHeight()
        {
            EnsureApplicationResources();

            var request = new HostedSurfaceRequest
            {
                SurfaceKind = HostedSurfaceKind.YamlDocument,
                ContentKey = "demo.yaml.generated-preview",
                PresentationMode = HostedPresentationMode.CompactModal,
                Size = HostedPresentationSize.Large,
                Placement = HostedSheetPlacement.Center,
                Title = "Generated Yaml form",
                Payload = BuildGeneratedPreviewPayload()
            };
            var content = new DemoYamlHostedSurfaceFactory().CreateContent(request, new NullHostedSurfaceManager());
            var modal = new PhialeModalLayerHost
            {
                Style = (Style)Application.Current.Resources[typeof(PhialeModalLayerHost)],
                HostedContent = content,
                IsSessionOpen = true,
                PresentationMode = HostedPresentationMode.CompactModal,
                SurfaceWidth = 900d,
                SurfaceMaxWidth = 1120d,
                SurfaceHorizontalAlignment = HorizontalAlignment.Center,
                SurfaceVerticalAlignment = VerticalAlignment.Center,
                SurfaceMargin = new Thickness(24d)
            };

            modal.Measure(new Size(1000d, 980d));
            modal.Arrange(new Rect(0d, 0d, 1000d, 980d));
            modal.ApplyTemplate();
            modal.UpdateLayout();

            var dialogRoot = FindDescendant<Border>(modal, "DialogRoot");

            Assert.That(dialogRoot, Is.Not.Null);
            Assert.That(dialogRoot.ActualHeight, Is.LessThan(820d));
        }

        [Test]
        public void HostedSurfaceFailureContent_ShouldAllowDismissingActiveModalSession()
        {
            var manager = new HostedSurfaceManager();
            var registry = new WpfHostedSurfaceFactoryRegistry();
            registry.Register(new ThrowingHostedSurfaceFactory());
            var service = new WpfHostedSurfaceService(manager, registry);
            var request = new HostedSurfaceRequest
            {
                SurfaceKind = HostedSurfaceKind.YamlDocument,
                ContentKey = "demo.yaml.generated-preview",
                PresentationMode = HostedPresentationMode.CompactModal,
                Size = HostedPresentationSize.Large,
                Placement = HostedSheetPlacement.Center,
                Title = "Generated Yaml form",
                CanDismiss = true,
                Payload = "broken"
            };

            var task = service.ShowAsync(request);
            var closeButton = FindDescendant<Button>(service.CurrentContent, "HostedSurfaceFailureCloseButton");

            Assert.That(closeButton, Is.Not.Null);

            closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.Result.Outcome, Is.EqualTo(HostedSurfaceResultOutcome.Dismissed));
            Assert.That(service.CurrentSession, Is.Null);
        }

        private static string BuildGeneratedPreviewPayload()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "namespace: application.forms",
                "imports:",
                "  - domain.person",
                "  - application.forms.actionShells",
                "",
                "document:",
                "  id: review-request",
                "  kind: Form",
                "  extends: review-sticky-header-footer",
                "  name: Review request document",
                "  topRegionChrome: Merged",
                "  bottomRegionChrome: Merged",
                "  header:",
                "    title: Review request",
                "    subtitle: Customer verification",
                "    description: Validate personal details and notes before completing the review workflow.",
                "    status: Draft",
                "    context: Internal form",
                "  footer:",
                "    note: Fields marked with * are required.",
                "    status: Draft saved locally",
                "    source: Demo YAML runtime",
                "  fields:",
                "    - id: firstName",
                "      extends: firstName",
                "    - id: lastName",
                "      extends: lastName",
                "    - id: age",
                "      extends: age",
                "    - id: notes",
                "      extends: documentNotes",
                "    - id: developer.firstName",
                "      extends: firstName",
                "    - id: developer.lastName",
                "      extends: lastName",
                "  layout:",
                "    type: Column",
                "    items:",
                "      - type: Container",
                "        caption: Reviewer",
                "        containerChrome: Framed",
                "        variant: Compact",
                "        items:",
                "          - type: Row",
                "            items:",
                "              - fieldRef: firstName",
                "              - fieldRef: lastName",
                "      - type: Container",
                "        caption: Developer data",
                "        containerChrome: Framed",
                "        containerBehavior: Collapsible",
                "        collapsedText: \"{developer.lastName} {developer.firstName}, {age}\"",
                "        variant: Compact",
                "        items:",
                "          - type: Row",
                "            items:",
                "              - fieldRef: developer.lastName",
                "              - fieldRef: developer.firstName",
                "              - fieldRef: age",
                "      - type: Container",
                "        caption: Review notes",
                "        containerChrome: None",
                "        items:",
                "          - fieldRef: notes",
            });
        }

        private static void EnsureApplicationResources()
        {
            if (Application.Current == null)
            {
                new Application();
            }

            var dictionaryPath = Path.Combine(
                GetRepositoryRoot(),
                "src",
                "PhialeTech",
                "Shared",
                "PhialeTech.Styles.Wpf",
                "Themes.Linked",
                "ComponentHost",
                "Generic.xaml");

            var xaml = File.ReadAllText(dictionaryPath)
                .Replace(
                    "xmlns:controls=\"clr-namespace:PhialeTech.ComponentHost.Wpf.Controls\"",
                    "xmlns:controls=\"clr-namespace:PhialeTech.ComponentHost.Wpf.Controls;assembly=PhialeTech.ComponentHost.Wpf\"");
            Application.Current.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(xaml));
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
            }

            return directory.FullName;
        }

        private static T FindDescendant<T>(DependencyObject parent, string name)
            where T : FrameworkElement
        {
            if (parent == null)
            {
                return null;
            }

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var typed = child as T;
                if (typed != null && string.Equals(typed.Name, name, StringComparison.Ordinal))
                {
                    return typed;
                }

                var nested = FindDescendant<T>(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private sealed class NullHostedSurfaceManager : IHostedSurfaceManager
        {
            public IHostedSurfaceSessionState CurrentSession => null;

            public event EventHandler CurrentSessionChanged
            {
                add { }
                remove { }
            }

            public Task<IHostedSurfaceResult> ShowAsync(IHostedSurfaceRequest request, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IHostedSurfaceResult>(null);
            }

            public bool TryConfirmCurrent(string commandId = null, string payload = null)
            {
                return true;
            }

            public bool TryCancelCurrent(string commandId = null, string payload = null)
            {
                return true;
            }

            public bool TryDismissCurrent(string commandId = null, string payload = null)
            {
                return true;
            }

            public void HandleCommand(UniversalCommandEventArgs e)
            {
            }

            public void HandleKey(UniversalKeyEventArgs e)
            {
            }

            public void HandlePointer(UniversalPointerRoutedEventArgs e)
            {
            }

            public void HandleFocus(UniversalFocusChangedEventArgs e)
            {
            }
        }

        private sealed class ThrowingHostedSurfaceFactory : IWpfHostedSurfaceFactory
        {
            public bool CanCreate(IHostedSurfaceRequest request)
            {
                return true;
            }

            public FrameworkElement CreateContent(IHostedSurfaceRequest request, IHostedSurfaceManager manager)
            {
                throw new InvalidOperationException("Broken hosted surface.");
            }
        }
    }
}
