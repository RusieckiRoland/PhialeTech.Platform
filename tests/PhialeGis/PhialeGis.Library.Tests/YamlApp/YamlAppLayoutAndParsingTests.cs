using System.Linq;
using NUnit.Framework;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Definitions.Layouts;
using PhialeTech.YamlApp.Infrastructure.Loading;

namespace PhialeGis.Library.Tests.YamlApp
{
    [TestFixture]
    public sealed class YamlAppLayoutAndParsingTests
    {
        [Test]
        public void ImportAndNormalize_ShouldHandleMinimalInlineForm()
        {
            var yaml = @"
id: MyFirstForm
kind: form

actions:
  - id: Ok
    kind: Ok
    caption: common.ok

  - id: Cancel
    kind: Cancel
    caption: common.cancel

layout:
  type: Column
  items:
    - type: Row
      items:
        - field:
            id: Name
            caption: person.name
";

            var imported = Import(yaml);
            var normalized = Normalize(imported);
            var importedForm = AsForm(imported.Definition);
            var normalizedForm = AsForm(normalized.Document);
            var root = normalized.Document.Layout;
            var row = root.Items.Single() as YamlRowDefinition;

            Assert.Multiple(() =>
            {
                Assert.That(imported.Definition.Id, Is.EqualTo("MyFirstForm"));
                Assert.That(importedForm.Actions.Count(), Is.EqualTo(2));
                Assert.That(importedForm.Actions.Select(a => a.Id), Is.EqualTo(new[] { "Ok", "Cancel" }));
                Assert.That(root, Is.TypeOf<YamlLayoutDefinition>());
                Assert.That(root.Name, Is.EqualTo("Column"));
                Assert.That(row, Is.Not.Null);
                Assert.That(normalizedForm.Fields.Select(f => f.Id), Is.EqualTo(new[] { "Name" }));
                Assert.That(row.Items.Single(), Is.TypeOf<YamlFieldReferenceDefinition>());
            });
        }

        [Test]
        public void Normalize_ShouldHandleReferencedFieldStyle_WithoutDuplication()
        {
            var yaml = @"
id: AddressForm
kind: form

fields:
  FirstName:
    caption: person.firstName
  LastName:
    caption: person.lastName
  Street:
    caption: address.street
  PostalCode:
    caption: address.postalCode
  City:
    caption: address.city

layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: FirstName
        - fieldRef: LastName
    - type: Row
      items:
        - fieldRef: Street
    - type: Row
      items:
        - fieldRef: PostalCode
        - fieldRef: City
";

            var normalized = Normalize(Import(yaml));
            var normalizedForm = AsForm(normalized.Document);
            var rows = normalized.Document.Layout.Items.Cast<YamlRowDefinition>().ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(normalizedForm.Fields.Count, Is.EqualTo(5));
                Assert.That(rows.Length, Is.EqualTo(3));
                Assert.That(rows[0].Items.Count, Is.EqualTo(2));
                Assert.That(rows[1].Items.Count, Is.EqualTo(1));
                Assert.That(rows[2].Items.Count, Is.EqualTo(2));
                Assert.That(normalized.Diagnostics, Is.Empty);
            });
        }

        [Test]
        public void Import_ShouldReportUnsupportedActionStripMode()
        {
            var yaml = @"
id: ActionStripDocument
actionStripMode: SharedFooter
fields:
  Name:
    caption: person.name
layout:
  type: Column
  items:
    - fieldRef: Name
";

            var imported = Import(yaml);

            Assert.That(imported.Diagnostics.Any(d => d.Contains("actionStripMode") && d.Contains("unsupported property")), Is.True);
        }

        [Test]
        public void Normalize_ShouldPromoteInlineFields_ToCanonicalFieldCollection()
        {
            var yaml = @"
id: InlineAddressForm
kind: form

layout:
  type: Column
  items:
    - type: Row
      items:
        - field:
            id: FirstName
            caption: person.firstName
        - field:
            id: LastName
            caption: person.lastName
    - type: Row
      items:
        - field:
            id: Street
            caption: address.street
    - type: Row
      items:
        - field:
            id: PostalCode
            caption: address.postalCode
        - field:
            id: City
            caption: address.city
";

            var imported = Import(yaml);
            var inlineFieldCount = CountInlineFields(imported.Definition.Layout);
            var normalized = Normalize(imported);
            var normalizedForm = AsForm(normalized.Document);
            var fieldRefs = CountFieldReferences(normalized.Document.Layout);

            Assert.Multiple(() =>
            {
                Assert.That(imported.Definition.Id, Is.EqualTo("InlineAddressForm"));
                Assert.That(inlineFieldCount, Is.EqualTo(5));
                Assert.That(normalizedForm.Fields.Count, Is.EqualTo(5));
                Assert.That(fieldRefs, Is.EqualTo(5));
                Assert.That(normalizedForm.Fields.Select(f => f.Id), Is.EqualTo(new[] { "FirstName", "LastName", "Street", "PostalCode", "City" }));
            });
        }

        [Test]
        public void Import_ShouldParseNestedContainer_WithCaptionAndBorder()
        {
            var yaml = @"
id: AddressContainerForm
kind: form

fields:
  FirstName:
    caption: person.firstName

layout:
  type: Column
  items:
    - type: Container
      id: AddressSection
      caption: address.details
      showBorder: true
      items:
        - type: Column
          items:
            - type: Row
              items:
                - fieldRef: FirstName
";

            var imported = Import(yaml);
            var root = imported.Definition.Layout;
            var container = root.Items.Single() as YamlContainerDefinition;
            var column = container.Items.Single() as YamlColumnDefinition;

            Assert.Multiple(() =>
            {
                Assert.That(root.Name, Is.EqualTo("Column"));
                Assert.That(root.Items.Count, Is.EqualTo(1));
                Assert.That(container, Is.Not.Null);
                Assert.That(container.Id, Is.EqualTo("AddressSection"));
                Assert.That(container.CaptionKey, Is.EqualTo("address.details"));
                Assert.That(container.ShowBorder, Is.True);
                Assert.That(column, Is.Not.Null);
            });
        }

        [Test]
        public void Import_ShouldParseActionAreasAndActionMetadata()
        {
            var yaml = @"
id: ActionAreaForm
kind: form
actionAreas:
  - id: footerPrimary
    placement: Bottom
    horizontalAlignment: Right
    shared: true
    sticky: false
actions:
  - id: ok
    semantic: Ok
    area: footerPrimary
    slot: End
    order: 100
fields:
  Name:
    caption: person.name
layout:
  type: Column
  items:
    - fieldRef: Name
";

            var imported = Import(yaml);
            var form = AsForm(imported.Definition);
            var actionArea = form.ActionAreas.Single();
            var action = form.Actions.Single();

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(actionArea.Id, Is.EqualTo("footerPrimary"));
                Assert.That(actionArea.Placement, Is.EqualTo(ActionPlacement.Bottom));
                Assert.That(actionArea.HorizontalAlignment, Is.EqualTo(ActionAlignment.Right));
                Assert.That(actionArea.Shared, Is.True);
                Assert.That(action.Semantic, Is.EqualTo(ActionSemantic.Ok));
                Assert.That(action.Area, Is.EqualTo("footerPrimary"));
                Assert.That(action.Slot, Is.EqualTo(ActionSlot.End));
                Assert.That(action.Order, Is.EqualTo(100));
            });
        }

        [Test]
        public void Normalize_ShouldAllowMixedInlineAndReferencedFields()
        {
            var yaml = @"
id: MixedForm
kind: form

fields:
  FirstName:
    caption: person.firstName

layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: FirstName
        - field:
            id: LastName
            caption: person.lastName
";

            var imported = Import(yaml);
            var normalized = Normalize(imported);
            var importedForm = AsForm(imported.Definition);
            var normalizedForm = AsForm(normalized.Document);
            var row = normalized.Document.Layout.Items.Single() as YamlRowDefinition;

            Assert.Multiple(() =>
            {
                Assert.That(importedForm.Fields.Count(), Is.EqualTo(1));
                Assert.That(row, Is.Not.Null);
                Assert.That(row.Items.Count, Is.EqualTo(2));
                Assert.That(normalizedForm.Fields.Count, Is.EqualTo(2));
                Assert.That(normalizedForm.Fields.Any(f => f.Id == "FirstName"), Is.True);
                Assert.That(normalizedForm.Fields.Any(f => f.Id == "LastName"), Is.True);
                Assert.That(row.Items.All(i => i is YamlFieldReferenceDefinition), Is.True);
            });
        }

        [Test]
        public void Normalize_ShouldFail_WhenRowIsEmpty()
        {
            var yaml = @"
id: InvalidForm
kind: form

layout:
  type: Column
  items:
    - type: Row
      items: []
";

            var imported = Import(yaml);
            var normalized = Normalize(imported);

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True);
                Assert.That(normalized.Success, Is.False);
                Assert.That(normalized.Diagnostics.Any(d => d.Contains("Row layout item must contain at least one child item.")), Is.True);
            });
        }

        [Test]
        public void Normalize_ShouldFail_WhenFieldReferenceTargetIsMissing()
        {
            var yaml = @"
id: MissingFieldRefForm
kind: form

fields:
  Name:
    caption: person.name

layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: UnknownField
";

            var normalized = Normalize(Import(yaml));

            Assert.Multiple(() =>
            {
                Assert.That(normalized.Success, Is.False);
                Assert.That(normalized.Diagnostics.Any(d => d.Contains("UnknownField")), Is.True);
            });
        }

        [Test]
        public void Normalize_ShouldPreserveNestedContainerTree()
        {
            var yaml = @"
id: NestedContainerForm
kind: form

fields:
  Name:
    caption: person.name

layout:
  type: Container
  id: Outer
  items:
    - type: Container
      id: Inner
      items:
        - type: Column
          items:
            - type: Row
              items:
                - fieldRef: Name
";

            var normalized = Normalize(Import(yaml));
            var resolved = normalized.ResolvedDocument.Layout;
            var outerColumn = resolved.Items.Single() as ResolvedContainerDefinition;
            var innerColumn = outerColumn.Items.Single() as ResolvedColumnDefinition;
            var row = innerColumn.Items.Single() as ResolvedRowDefinition;
            var field = row.Items.Single() as ResolvedFieldReferenceDefinition;

            Assert.Multiple(() =>
            {
                Assert.That(resolved.Id, Is.EqualTo("Outer"));
                Assert.That(outerColumn, Is.Not.Null);
                Assert.That(outerColumn.Id, Is.EqualTo("Inner"));
                Assert.That(innerColumn, Is.Not.Null);
                Assert.That(row, Is.Not.Null);
                Assert.That(field, Is.Not.Null);
                Assert.That(field.FieldRef, Is.EqualTo("Name"));
            });
        }

        [Test]
        public void Normalize_ShouldResolveActionAreaReferences()
        {
            var yaml = @"
id: ActionAreaResolveForm
kind: form
actionAreas:
  - id: footerBase
    placement: Bottom
    horizontalAlignment: Right
  - id: footerPrimary
    extends: footerBase
    sticky: true
actions:
  - id: ok
    semantic: Ok
    area: footerPrimary
fields:
  Name:
    caption: person.name
layout:
  type: Column
  items:
    - fieldRef: Name
";

            var normalized = Normalize(Import(yaml));
            var form = AsForm(normalized.Document);
            var resolved = AsResolvedForm(normalized.ResolvedDocument);
            var actionArea = form.ActionAreas.Single(item => item.Id == "footerPrimary");
            var action = resolved.Actions.Single();

            Assert.Multiple(() =>
            {
                Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));
                Assert.That(actionArea.Placement, Is.EqualTo(ActionPlacement.Bottom));
                Assert.That(actionArea.HorizontalAlignment, Is.EqualTo(ActionAlignment.Right));
                Assert.That(actionArea.Sticky, Is.True);
                Assert.That(action.Area, Is.EqualTo("footerPrimary"));
            });
        }

        [Test]
        public void Normalize_ShouldResolveLocalOverride_BeforeContainerAndFormDefaults()
        {
            var yaml = @"
id: OverrideForm
kind: form
interactionMode: Classic
densityMode: Comfortable
fieldChromeMode: Framed

layout:
  type: Column
  items:
    - type: Row
      interactionMode: Touch
      items:
        - field:
            id: Name
            caption: person.name
            interactionMode: Classic
            fieldChromeMode: InlineHint
";

            var normalized = Normalize(Import(yaml));
            var field = AsResolvedForm(normalized.ResolvedDocument).FieldMap["Name"];

            Assert.Multiple(() =>
            {
                Assert.That(field.InteractionMode, Is.EqualTo(InteractionMode.Classic));
                Assert.That(field.DensityMode, Is.EqualTo(DensityMode.Comfortable));
                Assert.That(field.FieldChromeMode, Is.EqualTo(FieldChromeMode.InlineHint));
            });
        }

        [Test]
        public void Normalize_ShouldInheritInteractionDensityAndChrome_FromLayout_WhenFieldDoesNotDefineThem()
        {
            var yaml = @"
id: InheritedPresentationForm
kind: form
interactionMode: Classic
densityMode: Compact
fieldChromeMode: Framed

layout:
  type: Column
  interactionMode: Touch
  densityMode: Normal
  fieldChromeMode: InlineHint
  items:
    - type: Row
      items:
        - field:
            id: Name
            caption: person.name
";

            var normalized = Normalize(Import(yaml));
            var field = AsResolvedForm(normalized.ResolvedDocument).FieldMap["Name"];

            Assert.Multiple(() =>
            {
                Assert.That(field.InteractionMode, Is.EqualTo(InteractionMode.Touch));
                Assert.That(field.DensityMode, Is.EqualTo(DensityMode.Normal));
                Assert.That(field.FieldChromeMode, Is.EqualTo(FieldChromeMode.InlineHint));
            });
        }

        [Test]
        public void Import_ShouldParseWidthHint_FromYaml()
        {
            var yaml = @"
id: WidthHintForm
widthHint: Fill

fields:
  Name:
    caption: person.name
    widthHint: Medium

layout:
  type: Column
  widthHint: Long
  items:
    - type: Row
      widthHint: Short
      items:
        - fieldRef: Name
";

            var imported = Import(yaml);
            var root = imported.Definition.Layout;
            var row = root.Items.Single() as YamlRowDefinition;
            var field = AsForm(imported.Definition).Fields.Single();

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(imported.Definition.WidthHint, Is.EqualTo(FieldWidthHint.Fill));
                Assert.That(root.WidthHint, Is.EqualTo(FieldWidthHint.Long));
                Assert.That(row.WidthHint, Is.EqualTo(FieldWidthHint.Short));
                Assert.That(field.WidthHint, Is.EqualTo(FieldWidthHint.Medium));
            });
        }

        [Test]
        public void Import_ShouldFail_WhenFieldDefinesWidthAndWidthHintTogether()
        {
            var yaml = @"
id: InvalidWidthForm
fields:
  Name:
    width: 240
    widthHint: Long
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
";

            var imported = Import(yaml);

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.False);
                Assert.That(imported.Diagnostics.Any(d => d.Contains("cannot define both 'width' and 'widthHint'")), Is.True);
            });
        }

        [Test]
        public void Import_ShouldParse_StringFieldMaxLength()
        {
            var yaml = @"
id: MaxLengthForm
fields:
  Name:
    caption: person.name
    maxLength: 50
layout:
  type: Column
  items:
    - fieldRef: Name
";

            var imported = Import(yaml);
            var field = AsForm(imported.Definition).Fields.OfType<YamlStringFieldDefinition>().Single(item => item.Id == "Name");

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(field.MaxLength, Is.EqualTo(50));
            });
        }

        [Test]
        public void Import_ShouldParse_DocumentEditorField_FromControlAlias()
        {
            var yaml = @"
id: DocumentEditorForm
fields:
  Notes:
    control: YamlDocumentEditor
    caption: notes.caption
    placeholder: notes.placeholder
layout:
  type: Column
  items:
    - fieldRef: Notes
";

            var imported = Import(yaml);
            var field = AsForm(imported.Definition).Fields.Single(item => item.Id == "Notes");

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(field.GetType().Name, Is.EqualTo("YamlDocumentEditorFieldDefinition"));
                Assert.That(field.PlaceholderKey, Is.EqualTo("notes.placeholder"));
            });
        }

        [Test]
        public void Import_ShouldParse_DocumentEditorField_FromValueTypeAlias()
        {
            var yaml = @"
id: DocumentEditorForm
fields:
  Description:
    valueType: richText
    caption: description.caption
layout:
  type: Column
  items:
    - fieldRef: Description
";

            var imported = Import(yaml);
            var field = AsForm(imported.Definition).Fields.Single(item => item.Id == "Description");

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(field.GetType().Name, Is.EqualTo("YamlDocumentEditorFieldDefinition"));
                Assert.That(field.CaptionKey, Is.EqualTo("description.caption"));
            });
        }

        [Test]
        public void Import_ShouldFail_WhenFieldMaxLengthIsInvalid()
        {
            var yaml = @"
id: InvalidMaxLengthForm
fields:
  Name:
    caption: person.name
    maxLength: wrong
layout:
  type: Column
  items:
    - fieldRef: Name
";

            var imported = Import(yaml);

            Assert.That(imported.Diagnostics.Any(d => d.Contains("maxLength") && d.Contains("non-negative integer")), Is.True);
        }

        private static YamlDefinitionImportResult<YamlDocumentDefinition> Import(string yaml)
        {
            return new YamlDocumentDefinitionImporter().Import(yaml);
        }

        private static YamlDocumentNormalizationResult Normalize(YamlDefinitionImportResult<YamlDocumentDefinition> imported)
        {
            Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
            return new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition);
        }

        private static YamlFormDocumentDefinition AsForm(YamlDocumentDefinition document)
        {
            Assert.That(document, Is.TypeOf<YamlFormDocumentDefinition>());
            return (YamlFormDocumentDefinition)document;
        }

        private static ResolvedFormDocumentDefinition AsResolvedForm(ResolvedDocumentDefinition document)
        {
            Assert.That(document, Is.TypeOf<ResolvedFormDocumentDefinition>());
            return (ResolvedFormDocumentDefinition)document;
        }

        private static int CountInlineFields(YamlLayoutDefinition layout)
        {
            return layout.Items.Sum(CountInlineFields);
        }

        private static int CountInlineFields(ILayoutItemDefinition item)
        {
            if (item is YamlInlineFieldLayoutItemDefinition)
            {
                return 1;
            }

            if (item is YamlRowDefinition row)
            {
                return row.Items.Sum(CountInlineFields);
            }

            if (item is YamlColumnDefinition column)
            {
                return column.Items.Sum(CountInlineFields);
            }

            if (item is YamlContainerDefinition container)
            {
                return container.Items.Sum(CountInlineFields);
            }

            return 0;
        }

        private static int CountFieldReferences(YamlLayoutDefinition layout)
        {
            return layout.Items.Sum(CountFieldReferences);
        }

        private static int CountFieldReferences(ILayoutItemDefinition item)
        {
            if (item is YamlFieldReferenceDefinition)
            {
                return 1;
            }

            if (item is YamlRowDefinition row)
            {
                return row.Items.Sum(CountFieldReferences);
            }

            if (item is YamlColumnDefinition column)
            {
                return column.Items.Sum(CountFieldReferences);
            }

            if (item is YamlContainerDefinition container)
            {
                return container.Items.Sum(CountFieldReferences);
            }

            return 0;
        }
    }
}


