using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Infrastructure.Loading;
using PhialeTech.YamlApp.Runtime.Services;
using PhialeTech.YamlApp.Wpf.Document;

namespace PhialeTech.WebHost.Wpf.Tests
{
    public sealed class YamlContainerChromeRenderingTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ContainerChromeNone_DoesNotRenderContainerCaption()
        {
            var yaml = @"
id: ContainerChromeNoneForm
kind: form

fields:
  Notes:
    control: YamlTextBox
    caption: Notes

layout:
  type: Column
  items:
    - type: Container
      caption: Hidden container caption
      containerChrome: None
      items:
        - fieldRef: Notes
";

            var imported = new YamlDocumentDefinitionImporter().Import(yaml);
            Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition);
            Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));

            var runtime = new RuntimeDocumentStateFactory().Create((ResolvedFormDocumentDefinition)normalized.ResolvedDocument);
            var element = new YamlDocumentLayoutRenderer().Render(runtime);
            var textBlocks = FindVisualChildren<TextBlock>(element).Select(text => text.Text).ToArray();

            Assert.That(textBlocks, Does.Not.Contain("Hidden container caption"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ContainerBehaviorCollapsible_RendersExpandedContainer()
        {
            var yaml = @"
id: CollapsibleContainerForm
kind: form

fields:
  Notes:
    control: YamlTextBox
    caption: Notes

layout:
  type: Column
  items:
    - type: Container
      caption: Review notes
      containerChrome: Framed
      containerBehavior: Collapsible
      items:
        - fieldRef: Notes
";

            var imported = new YamlDocumentDefinitionImporter().Import(yaml);
            Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition);
            Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));

            var runtime = new RuntimeDocumentStateFactory().Create((ResolvedFormDocumentDefinition)normalized.ResolvedDocument);
            var element = new YamlDocumentLayoutRenderer().Render(runtime);
            var expander = FindVisualChildren<Expander>(element).Single();

            Assert.Multiple(() =>
            {
                Assert.That(((TextBlock)((StackPanel)expander.Header).Children[0]).Text, Is.EqualTo("Review notes"));
                Assert.That(expander.IsExpanded, Is.True);
                Assert.That(expander.ReadLocalValue(FrameworkElement.StyleProperty), Is.Not.EqualTo(DependencyProperty.UnsetValue));
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ContainerBehaviorCollapsible_ShowsCollapsedText_WhenCollapsed()
        {
            var yaml = @"
id: CollapsibleContainerSummaryForm
kind: form

fields:
  firstName:
    control: YamlTextBox
    caption: First name
    value: Ada
  age:
    control: YamlIntegerBox
    caption: Age
    value: 37

layout:
  type: Column
  items:
    - type: Container
      caption: Developer data
      containerChrome: Framed
      containerBehavior: Collapsible
      collapsedText: ""Developer: {firstName} {age}""
      items:
        - type: Row
          items:
            - fieldRef: firstName
            - fieldRef: age
";

            var imported = new YamlDocumentDefinitionImporter().Import(yaml);
            Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition);
            Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));

            var runtime = new RuntimeDocumentStateFactory().Create((ResolvedFormDocumentDefinition)normalized.ResolvedDocument);
            runtime.GetField("firstName").SetValue("Ada");
            runtime.GetField("age").SetValue(37);
            var element = new YamlDocumentLayoutRenderer().Render(runtime);
            var expander = FindVisualChildren<Expander>(element).Single();

            expander.IsExpanded = false;

            var headerText = ((StackPanel)expander.Header).Children.OfType<TextBlock>()
                .Select(text => text.Text)
                .ToArray();

            Assert.That(headerText, Does.Contain("Developer: Ada 37"));

            runtime.GetField("age").SetValue(38);

            headerText = ((StackPanel)expander.Header).Children.OfType<TextBlock>()
                .Select(text => text.Text)
                .ToArray();

            Assert.That(headerText, Does.Contain("Developer: Ada 38"));
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
