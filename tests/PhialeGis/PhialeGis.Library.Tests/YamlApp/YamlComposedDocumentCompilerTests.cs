using System;
using System.Linq;
using NUnit.Framework;
using PhialeTech.Yaml.Library;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Infrastructure.Loading;

namespace PhialeGis.Library.Tests.YamlApp
{
    [TestFixture]
    public sealed class YamlComposedDocumentCompilerTests
    {
        [Test]
        [Category("Unit")]
        public void Compile_ShouldResolveImportsExtendsAndLocalization_ForEnglish()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    kind: Form
    name: YAML generated form
    interactionMode: Classic
    densityMode: Normal
    fieldChromeMode: Framed
    fields:
      - id: firstName
        extends: firstName
      - id: lastName
        extends: lastName
      - id: age
        extends: age
      - id: notes
        extends: notes
    layout:
      type: Column
      items:
        - type: Row
          items:
            - fieldRef: firstName
            - fieldRef: lastName
            - fieldRef: age
        - fieldRef: notes
    actions:
      - id: ok
        kind: Ok
        captionKey: actions.ok.caption
      - id: cancel
        kind: Cancel
        captionKey: actions.cancel.caption
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                Assert.That(compiled.Definition.Id, Is.EqualTo("yaml-generated-form"));
                Assert.That(compiledForm.Fields.Select(f => f.Id), Is.EqualTo(new[] { "firstName", "lastName", "age", "notes" }));

                var firstName = compiledForm.Fields.Single(f => string.Equals(f.Id, "firstName", StringComparison.OrdinalIgnoreCase));
                var age = compiledForm.Fields.Single(f => string.Equals(f.Id, "age", StringComparison.OrdinalIgnoreCase));
                var notes = compiledForm.Fields.Single(f => string.Equals(f.Id, "notes", StringComparison.OrdinalIgnoreCase));
                var okAction = compiledForm.Actions.Single(a => string.Equals(a.Id, "ok", StringComparison.OrdinalIgnoreCase));
                var cancelAction = compiledForm.Actions.Single(a => string.Equals(a.Id, "cancel", StringComparison.OrdinalIgnoreCase));

                Assert.That(firstName.CaptionKey, Is.EqualTo("First name"));
                Assert.That(firstName.PlaceholderKey, Is.EqualTo("Enter first name"));
                Assert.That(firstName.IsRequired, Is.True);
                Assert.That(age.CaptionKey, Is.EqualTo("Age"));
                Assert.That(age.PlaceholderKey, Is.EqualTo("Enter age"));
                Assert.That(notes.CaptionKey, Is.EqualTo("Notes"));
                Assert.That(notes.PlaceholderKey, Is.EqualTo("Enter additional information"));
                Assert.That(okAction.CaptionKey, Is.EqualTo("OK"));
                Assert.That(cancelAction.CaptionKey, Is.EqualTo("Cancel"));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldApplyLocalFieldOverrides_OnTopOfLibraryDefinition()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    kind: Form
    name: YAML generated form
    interactionMode: Classic
    densityMode: Normal
    fieldChromeMode: Framed
    fields:
      - id: customerGivenName
        extends: firstName
        captionKey: person.lastName.caption
        placeholderKey: person.lastName.placeholder
      - id: notes
        extends: notes
    layout:
      type: Column
      items:
        - fieldRef: customerGivenName
        - fieldRef: notes
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "pl");
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                var field = compiledForm.Fields.Single(f => string.Equals(f.Id, "customerGivenName", StringComparison.OrdinalIgnoreCase));
                Assert.That(field.CaptionKey, Is.EqualTo("Nazwisko"));
                Assert.That(field.PlaceholderKey, Is.EqualTo("Wpisz nazwisko"));
                Assert.That(field.IsRequired, Is.True);
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldInferExtendsFromFieldId_WhenDefinitionWithSameNameExists()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    kind: Form
    fields:
      - id: firstName
      - id: notes
    layout:
      type: Column
      items:
        - fieldRef: firstName
        - fieldRef: notes
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                var firstName = compiledForm.Fields.Single(f => string.Equals(f.Id, "firstName", StringComparison.OrdinalIgnoreCase));
                var notes = compiledForm.Fields.Single(f => string.Equals(f.Id, "notes", StringComparison.OrdinalIgnoreCase));
                Assert.That(firstName.CaptionKey, Is.EqualTo("First name"));
                Assert.That(notes.CaptionKey, Is.EqualTo("Notes"));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldSupportSingleDocumentSection_WithExplicitId()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

document:
  id: yaml-generated-form
  kind: Form
  fields:
    - id: firstName
  layout:
    type: Column
    items:
      - fieldRef: firstName
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(compiledForm.Id, Is.EqualTo("yaml-generated-form"));
                Assert.That(compiledForm.Fields.Single().Id, Is.EqualTo("firstName"));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldAllowDocumentWithoutFields_WhenItOnlyDefinesActions()
        {
            var yaml = @"
namespace: application.forms

document:
  id: action-only-form
  kind: Form
  actionAreas:
    - id: footerPrimary
      placement: Bottom
      horizontalAlignment: Right
      shared: true
      sticky: true
  actions:
    - id: save
      semantic: Ok
      caption: Save
      area: footerPrimary
      isPrimary: true
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var compiledForm = AsForm(compiled.Definition);
            var normalizedForm = AsForm(normalized.Document);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                Assert.That(compiledForm.Fields, Is.Empty);
                Assert.That(compiledForm.Actions.Select(a => a.Id), Is.EqualTo(new[] { "save" }));
                Assert.That(normalizedForm.Fields, Is.Empty);
                Assert.That(normalizedForm.Layout, Is.Null);
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldResolveDocumentExtends_FromImportedYamlLibraryDocument()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person
  - application.forms.actionShells

document:
  id: review-request
  kind: Form
  extends: review-sticky-header-footer
  fields:
    - id: firstName
      extends: firstName
    - id: notes
      extends: notes
  layout:
    type: Column
    items:
      - fieldRef: firstName
      - fieldRef: notes
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                Assert.That(compiledForm.Fields.Select(f => f.Id), Is.EqualTo(new[] { "firstName", "notes" }));
                Assert.That(compiledForm.Actions.Any(a => string.Equals(a.Id, "save", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(compiledForm.Actions.Any(a => string.Equals(a.Id, "cancel", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(compiledForm.ActionAreas.Any(a => string.Equals(a.Id, "headerActions", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(compiledForm.ActionAreas.Any(a => string.Equals(a.Id, "footerPrimary", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(compiledForm.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Auto));
                Assert.That(normalized.ResolvedDocument.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Auto));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldAllowDerivedDocumentToOverrideInheritedLayoutHeightMode()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person
  - application.forms.actionShells

document:
  id: full-height-review-request
  kind: Form
  extends: review-sticky-header-footer
  fields:
    - id: firstName
      extends: firstName
  layout:
    type: Column
    heightMode: Fill
    items:
      - fieldRef: firstName
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                Assert.That(compiledForm.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Fill));
                Assert.That(normalized.ResolvedDocument.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Fill));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldResolveGeneratedFormHeightMode_FromYamlLibraryHierarchy()
        {
            var yaml = @"
namespace: application.forms.tests
imports:
  - application.forms

document:
  id: derived-generated-form
  kind: Form
  extends: yaml-generated-form
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var compiledForm = AsForm(compiled.Definition);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                Assert.That(compiledForm.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Auto));
                Assert.That(normalized.ResolvedDocument.Layout.HeightMode, Is.EqualTo(LayoutHeightMode.Auto));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldReturnDiagnostics_WhenDefinitionCannotBeResolved()
        {
            var yaml = @"
namespace: application.forms

documents:
  - id: invalid-form
    name: Invalid form
    fields:
      - id: firstName
        extends: missingDefinition
    layout:
      type: Column
      items:
        - fieldRef: firstName
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.False);
                Assert.That(compiled.Definition, Is.Not.Null);
                Assert.That(compiled.Diagnostics.Any(d => d.IndexOf("missingDefinition", StringComparison.OrdinalIgnoreCase) >= 0), Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
            });
        }

        [Test]
        [Category("Unit")]
    public void Compile_ShouldApplyLocalMaxLengthOverride_OnTopOfLibraryDefinition()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    name: YAML generated form
    fields:
      - id: firstName
        extends: firstName
        maxLength: 3
    layout:
      type: Column
      items:
        - fieldRef: firstName
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var normalizedForm = AsForm(normalized.Document);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                var field = normalizedForm.Fields.OfType<YamlStringFieldDefinition>().Single(f => string.Equals(f.Id, "firstName", StringComparison.OrdinalIgnoreCase));
                Assert.That(field.MaxLength, Is.EqualTo(3));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldResolveIntegerDefinition_FromYamlLibrary()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    name: YAML generated form
    fields:
      - id: age
        extends: age
        minValue: 18
    layout:
      type: Column
      items:
        - fieldRef: age
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var normalizedForm = AsForm(normalized.Document);

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                var field = normalizedForm.Fields.OfType<YamlIntegerFieldDefinition>().Single(f => string.Equals(f.Id, "age", StringComparison.OrdinalIgnoreCase));
                Assert.That(field.MinValue, Is.EqualTo(18));
                Assert.That(field.MaxValue, Is.Null);
                Assert.That(field.CaptionKey, Is.EqualTo("Age"));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldResolveDocumentNotesDefinition_FromYamlLibrary()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    name: YAML generated form
    fields:
      - id: notes
        extends: documentNotes
    layout:
      type: Column
      items:
        - fieldRef: notes
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var normalizedForm = AsForm(normalized.Document);
            const string EmptyDocumentJson = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\"}]}";

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                var field = normalizedForm.Fields.OfType<YamlDocumentEditorFieldDefinition>().Single(f => string.Equals(f.Id, "notes", StringComparison.OrdinalIgnoreCase));
                Assert.That(field.CaptionKey, Is.EqualTo("Notes"));
                Assert.That(field.PlaceholderKey, Is.EqualTo("Enter additional information"));
                Assert.That(field.WidthHint, Is.EqualTo(PhialeTech.YamlApp.Abstractions.Enums.FieldWidthHint.Fill));
                Assert.That(field.FieldChromeMode, Is.EqualTo(PhialeTech.YamlApp.Abstractions.Enums.FieldChromeMode.Framed));
                Assert.That(field.CaptionPlacement, Is.EqualTo(PhialeTech.YamlApp.Abstractions.Enums.CaptionPlacement.Top));
                Assert.That(field.Value, Is.EqualTo(EmptyDocumentJson));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldResolveActionAreasAndActions_WithExtends()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

definitions:
  footerBaseArea:
    placement: Bottom
    horizontalAlignment: Right
    shared: true

  primaryFooterArea:
    extends: footerBaseArea
    sticky: true

  okActionBase:
    semantic: Ok
    slot: End

documents:
  - id: yaml-generated-form
    kind: Form
    name: YAML generated form
    actionAreas:
      - id: footerPrimary
        extends: primaryFooterArea
    fields:
      - id: firstName
        extends: firstName
    layout:
      type: Column
      items:
        - fieldRef: firstName
    actions:
      - id: ok
        extends: okActionBase
        captionKey: actions.ok.caption
        area: footerPrimary
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(compiled.Definition);
            var form = AsForm(normalized.Document);
            var resolved = (PhialeTech.YamlApp.Core.Resolved.ResolvedFormDocumentDefinition)normalized.ResolvedDocument;

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join(Environment.NewLine, normalized.Diagnostics));
                Assert.That(form.ActionAreas.Single().Sticky, Is.True);
                Assert.That(form.ActionAreas.Single().Placement, Is.EqualTo(PhialeTech.YamlApp.Abstractions.Enums.ActionPlacement.Bottom));
                Assert.That(form.Actions.Single().Semantic, Is.EqualTo(PhialeTech.YamlApp.Abstractions.Enums.ActionSemantic.Ok));
                Assert.That(form.Actions.Single().Area, Is.EqualTo("footerPrimary"));
                Assert.That(resolved.Actions.Single().Area, Is.EqualTo("footerPrimary"));
            });
        }

        [Test]
        [Category("Unit")]
        public void Compile_ShouldReportLineAndColumn_WhenYamlSyntaxIsInvalid()
        {
            var yaml = @"
namespace: application.forms
imports:
  - domain.person

documents:
  - id: yaml-generated-form
    name: YAML generated form
    fields:
      - id: firstName
        extends: firstName
        maxLength:3
    layout:
      type: Column
      items:
        - fieldRef: firstName
";

            var compiler = new YamlComposedDocumentCompiler();
            var compiled = compiler.Compile(yaml, new[] { typeof(YamlLibraryMarker).Assembly }, "en");

            Assert.Multiple(() =>
            {
                Assert.That(compiled.Success, Is.False);
                Assert.That(compiled.Diagnostics.Any(d =>
                    d.IndexOf("line", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    d.IndexOf("column", StringComparison.OrdinalIgnoreCase) >= 0), Is.True, string.Join(Environment.NewLine, compiled.Diagnostics));
            });
        }

        [Test]
        [Category("Unit")]
        public void LibraryAssembly_ShouldEmbedYamlDefinitionAndLocalizationResources()
        {
            var resourceNames = typeof(YamlLibraryMarker).Assembly.GetManifestResourceNames();

            Assert.Multiple(() =>
            {
                Assert.That(resourceNames.Any(name => name.EndsWith("Definitions.bases.text.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Definitions.bases.numeric.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Definitions.domain.person.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Definitions.medium.numeric.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Definitions.application.forms.generated-form.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Definitions.application.forms.actionShells.confirm-footer-right.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Localization.en.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
                Assert.That(resourceNames.Any(name => name.EndsWith("Localization.pl.yaml", StringComparison.OrdinalIgnoreCase)), Is.True);
            });
        }

        private static YamlFormDocumentDefinition AsForm(YamlDocumentDefinition document)
        {
            Assert.That(document, Is.TypeOf<YamlFormDocumentDefinition>());
            return (YamlFormDocumentDefinition)document;
        }
    }
}

