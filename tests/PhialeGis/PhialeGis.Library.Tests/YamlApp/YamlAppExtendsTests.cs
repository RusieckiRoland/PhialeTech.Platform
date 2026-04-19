using System.Linq;
using NUnit.Framework;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Definitions.Documents;
using PhialeTech.YamlApp.Infrastructure.Loading;

namespace PhialeGis.Library.Tests.YamlApp
{
    [TestFixture]
    public sealed class YamlAppExtendsTests
    {
        [Test]
        public void Normalize_ShouldResolveFieldExtends_FromFormFieldDefinitions()
        {
            var importer = new YamlDocumentDefinitionImporter();
            var yaml = @"
id: MyForm
fields:
  BaseString:
    placeholder: base.placeholder
    enabled: false
  Name:
    extends: BaseString
    caption: person.name
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
";

            var imported = importer.Import(yaml);
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition as YamlDocumentDefinition);
            var nameField = AsForm(normalized.Document).Fields.Single(f => f.Id == "Name");

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));
                Assert.That(nameField.Extends, Is.EqualTo("BaseString"));
                Assert.That(nameField.CaptionKey, Is.EqualTo("person.name"));
                Assert.That(nameField.PlaceholderKey, Is.EqualTo("base.placeholder"));
                Assert.That(nameField.Enabled, Is.False);
            });
        }

        [Test]
        public void Normalize_ShouldAllowDerivedField_ToOverrideWidth_WithWidthHint()
        {
            var importer = new YamlDocumentDefinitionImporter();
            var yaml = @"
id: MyForm
fields:
  BaseString:
    width: 240
  Name:
    extends: BaseString
    widthHint: Long
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
";

            var imported = importer.Import(yaml);
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition as YamlDocumentDefinition);
            var nameField = AsForm(normalized.Document).Fields.Single(f => f.Id == "Name");

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));
                Assert.That(nameField.Width, Is.Null);
                Assert.That(nameField.WidthHint, Is.EqualTo(PhialeTech.YamlApp.Abstractions.Enums.FieldWidthHint.Long));
            });
        }

        [Test]
        public void Normalize_ShouldInheritMaxLength_FromBaseField()
        {
            var importer = new YamlDocumentDefinitionImporter();
            var yaml = @"
id: MyForm
fields:
  BaseString:
    maxLength: 50
  Name:
    extends: BaseString
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
";

            var imported = importer.Import(yaml);
            var normalized = new YamlDocumentDefinitionNormalizer().Normalize(imported.Definition as YamlDocumentDefinition);
            var nameField = AsForm(normalized.Document).Fields.OfType<YamlStringFieldDefinition>().Single(f => f.Id == "Name");

            Assert.Multiple(() =>
            {
                Assert.That(imported.Success, Is.True, string.Join("\n", imported.Diagnostics));
                Assert.That(normalized.Success, Is.True, string.Join("\n", normalized.Diagnostics));
                Assert.That(nameField.MaxLength, Is.EqualTo(50));
            });
        }

        private static YamlFormDocumentDefinition AsForm(YamlDocumentDefinition document)
        {
            Assert.That(document, Is.TypeOf<YamlFormDocumentDefinition>());
            return (YamlFormDocumentDefinition)document;
        }
    }
}


