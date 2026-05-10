using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Core.Normalization;
using PhialeTech.YamlApp.Infrastructure.Configuration;
using PhialeTech.YamlApp.Runtime.Model;
using PhialeTech.YamlApp.Runtime.Services;

namespace PhialeGis.Library.Tests.YamlApp
{
    [TestFixture]
    public sealed class YamlAppPipelineTests
    {
        [Test]
        public void Prepare_ShouldLoadNormalizeAndMapJson_WithInheritedEnabledState()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlNameConfig.yaml"), @"
id: MyForm
kind: form
enabled: false
fields:
  Name:
    caption: person.name
actions:
  - id: Ok
    kind: Ok
    caption: common.ok
  - id: Cancel
    kind: Cancel
    caption: common.cancel
    enabled: true
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
");

                var configurationSource = new FilesystemYamlDocumentConfigurationSource(configurationRoot);
                var preparationService = new DocumentRuntimePreparationService(
                    configurationSource,
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlNameConfig", "{\"MyForm\":{\"Name\":\"Andrzej\"}}");
                var field = result.Runtime.GetField("Name");
                var okAction = result.Runtime.Actions.Single(a => a.Action.ActionKind == DocumentActionKind.Ok);
                var cancelAction = result.Runtime.Actions.Single(a => a.Action.ActionKind == DocumentActionKind.Cancel);
                var outputJson = new RuntimeDocumentJsonMapper().ToJson(result.Runtime);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(result.Normalization.Success, Is.True, string.Join(Environment.NewLine, result.Normalization.Diagnostics));
                    Assert.That(result.Runtime, Is.Not.Null);
                    Assert.That(result.Runtime.Id, Is.EqualTo("MyForm"));

                    Assert.That(field, Is.Not.Null);
                    Assert.That(field.Value, Is.EqualTo("Andrzej"));
                    Assert.That(field.Enabled, Is.False);
                    Assert.That(field.Visible, Is.True);
                    Assert.That(field.IsTouched, Is.False);
                    Assert.That(field.IsDirty, Is.False);

                    Assert.That(okAction.Enabled, Is.False);
                    Assert.That(cancelAction.Enabled, Is.True);

                    Assert.That(outputJson, Is.EqualTo("{\"MyForm\":{\"Name\":\"Andrzej\"}}"));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void Prepare_ShouldReturnDocumentBasedContracts_AcrossConfigurationNormalizationAndRuntime()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlDocumentContractConfig.yaml"), @"
id: CustomerDocument
kind: form
fields:
  Name:
    caption: person.name
actions:
  - id: Ok
    kind: Ok
    caption: common.ok
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
");

                var preparationService = new DocumentRuntimePreparationService(
                    new FilesystemYamlDocumentConfigurationSource(configurationRoot),
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlDocumentContractConfig");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(result.Configuration, Is.InstanceOf<IDocumentDefinition>());
                    Assert.That(result.ConfigurationName, Is.EqualTo("YamlDocumentContractConfig"));
                    Assert.That(result.Normalization.Document, Is.Not.Null);
                    Assert.That(result.Normalization.Document.Id, Is.EqualTo("CustomerDocument"));
                    Assert.That(result.Normalization.ResolvedDocument, Is.Not.Null);
                    Assert.That(result.Normalization.ResolvedDocument.Id, Is.EqualTo("CustomerDocument"));
                    Assert.That(result.Runtime, Is.Not.Null);
                    Assert.That(result.Runtime.Document, Is.Not.Null);
                    Assert.That(result.Runtime.Document.Id, Is.EqualTo("CustomerDocument"));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void Prepare_ShouldAllowFormWithoutLayout_WhenDocumentOnlyDefinesActions()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlActionOnlyConfig.yaml"), @"
id: ActionOnlyDocument
kind: form
actions:
  - id: Save
    kind: Ok
    caption: common.ok
  - id: Cancel
    kind: Cancel
    caption: common.cancel
");

                var preparationService = new DocumentRuntimePreparationService(
                    new FilesystemYamlDocumentConfigurationSource(configurationRoot),
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlActionOnlyConfig");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(result.Normalization.Success, Is.True, string.Join(Environment.NewLine, result.Normalization.Diagnostics));
                    Assert.That(result.Runtime, Is.Not.Null);
                    Assert.That(result.Runtime.Document, Is.Not.Null);
                    Assert.That(result.Runtime.Document.Layout, Is.Null);
                    Assert.That(result.Runtime.Fields, Is.Empty);
                    Assert.That(result.Runtime.Actions.Select(a => a.Action.Id), Is.EqualTo(new[] { "Save", "Cancel" }));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void RuntimeDocumentJsonMapper_ToConfirmedResult_ShouldReturnDocumentDialogResult_WithDocumentIdAndJson()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlDocumentResultConfig.yaml"), @"
id: AddressDocument
kind: form
fields:
  City:
    caption: address.city
actions:
  - id: Ok
    kind: Ok
    caption: common.ok
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: City
");

                var preparationService = new DocumentRuntimePreparationService(
                    new FilesystemYamlDocumentConfigurationSource(configurationRoot),
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlDocumentResultConfig", "{\"AddressDocument\":{\"City\":\"Warsaw\"}}");
                var dialogResult = new RuntimeDocumentJsonMapper().ToConfirmedResult(result.Runtime);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(dialogResult.DocumentId, Is.EqualTo("AddressDocument"));
                    Assert.That(dialogResult.IsConfirmed, Is.True);
                    Assert.That(dialogResult.IsCancelled, Is.False);
                    Assert.That(dialogResult.Json, Is.EqualTo("{\"AddressDocument\":{\"City\":\"Warsaw\"}}"));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void RuntimeDocumentJsonMapper_ToJson_ShouldUseDocumentFallbackKey_WhenStateIdIsMissing()
        {
            var mapper = new RuntimeDocumentJsonMapper();
            var runtime = new RuntimeDocumentState(
                document: null,
                fields: new[]
                {
                    new PhialeTech.YamlApp.Runtime.Model.RuntimeFieldState(null)
                },
                actions: Array.Empty<PhialeTech.YamlApp.Runtime.Model.RuntimeActionState>());

            var json = mapper.ToJson(runtime);

            Assert.That(json, Is.EqualTo("{\"Document\":{}}"));
        }

        [Test]
        public void Prepare_ShouldInheritPresentationOptions_FromFormAndLayoutContainers()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlPresentationConfig.yaml"), @"
id: MyForm
kind: form
validationTrigger: OnDebouncedChange
densityMode: Comfortable
fieldChromeMode: InlineHint
layout:
  type: Column
  items:
    - type: Row
      interactionMode: Touch
      items:
        - field:
            id: Name
            caption: person.name
");

                var configurationSource = new FilesystemYamlDocumentConfigurationSource(configurationRoot);
                var preparationService = new DocumentRuntimePreparationService(
                    configurationSource,
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlPresentationConfig");
                var field = result.Runtime.GetField("Name");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(field, Is.Not.Null);
                    Assert.That(field.Field.ValidationTrigger, Is.EqualTo(ValidationTrigger.OnDebouncedChange));
                    Assert.That(field.Field.DensityMode, Is.EqualTo(DensityMode.Comfortable));
                    Assert.That(field.Field.FieldChromeMode, Is.EqualTo(FieldChromeMode.InlineHint));
                    Assert.That(field.Field.InteractionMode, Is.EqualTo(InteractionMode.Touch));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void Prepare_ShouldInheritAndAllowOverride_ForShowOldValueRestoreButton()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlOldValueRestoreConfig.yaml"), @"
id: MyForm
kind: form
showOldValueRestoreButton: true
fields:
  Name:
    caption: person.name
  Description:
    caption: person.description
    showOldValueRestoreButton: false
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
        - fieldRef: Description
");

                var configurationSource = new FilesystemYamlDocumentConfigurationSource(configurationRoot);
                var preparationService = new DocumentRuntimePreparationService(
                    configurationSource,
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlOldValueRestoreConfig");
                var nameField = result.Runtime.GetField("Name");
                var descriptionField = result.Runtime.GetField("Description");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(nameField, Is.Not.Null);
                    Assert.That(descriptionField, Is.Not.Null);
                    Assert.That(nameField.ShowOldValueRestoreButton, Is.True);
                    Assert.That(nameField.Field.ShowOldValueRestoreButton, Is.True);
                    Assert.That(descriptionField.ShowOldValueRestoreButton, Is.False);
                    Assert.That(descriptionField.Field.ShowOldValueRestoreButton, Is.False);
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void Prepare_ShouldInheritAndAllowOverride_ForCaptionPlacement()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlCaptionPlacementConfig.yaml"), @"
id: MyForm
kind: form
captionPlacement: Left
fields:
  Name:
    caption: person.name
  Description:
    caption: person.description
    captionPlacement: Top
layout:
  type: Column
  items:
    - type: Row
      items:
        - fieldRef: Name
        - fieldRef: Description
");

                var configurationSource = new FilesystemYamlDocumentConfigurationSource(configurationRoot);
                var preparationService = new DocumentRuntimePreparationService(
                    configurationSource,
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlCaptionPlacementConfig");
                var nameField = result.Runtime.GetField("Name");
                var descriptionField = result.Runtime.GetField("Description");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(nameField, Is.Not.Null);
                    Assert.That(descriptionField, Is.Not.Null);
                    Assert.That(nameField.CaptionPlacement, Is.EqualTo(CaptionPlacement.Left));
                    Assert.That(nameField.Field.CaptionPlacement, Is.EqualTo(CaptionPlacement.Left));
                    Assert.That(descriptionField.CaptionPlacement, Is.EqualTo(CaptionPlacement.Top));
                    Assert.That(descriptionField.Field.CaptionPlacement, Is.EqualTo(CaptionPlacement.Top));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }

        [Test]
        public void Prepare_ShouldInheritInteractionDensityAndChrome_FromParentLayout_WhenFieldDoesNotDefineThem()
        {
            var configurationRoot = Path.Combine(Path.GetTempPath(), "YamlAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(configurationRoot);

            try
            {
                File.WriteAllText(Path.Combine(configurationRoot, "YamlInheritedPresentationConfig.yaml"), @"
id: MyForm
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
");

                var configurationSource = new FilesystemYamlDocumentConfigurationSource(configurationRoot);
                var preparationService = new DocumentRuntimePreparationService(
                    configurationSource,
                    new YamlDocumentDefinitionNormalizer(),
                    new RuntimeDocumentStateFactory(),
                    new RuntimeDocumentJsonMapper());

                var result = preparationService.Prepare("YamlInheritedPresentationConfig");
                var field = result.Runtime.GetField("Name");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.True, string.Join(Environment.NewLine, result.Diagnostics));
                    Assert.That(field, Is.Not.Null);
                    Assert.That(field.Field.InteractionMode, Is.EqualTo(InteractionMode.Touch));
                    Assert.That(field.Field.DensityMode, Is.EqualTo(DensityMode.Normal));
                    Assert.That(field.Field.FieldChromeMode, Is.EqualTo(FieldChromeMode.InlineHint));
                });
            }
            finally
            {
                if (Directory.Exists(configurationRoot))
                {
                    Directory.Delete(configurationRoot, recursive: true);
                }
            }
        }
    }
}




