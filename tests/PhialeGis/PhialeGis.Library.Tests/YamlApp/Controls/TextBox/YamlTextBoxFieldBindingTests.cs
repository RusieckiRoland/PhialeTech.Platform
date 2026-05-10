using NUnit.Framework;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Definitions.Fields;
using PhialeTech.YamlApp.Core.Resolved;
using PhialeTech.YamlApp.Runtime.Controls.TextBox;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeGis.Library.Tests.YamlApp.Controls.TextBox
{
    [TestFixture]
    public sealed class YamlTextBoxFieldBindingTests
    {
        [Test]
        public void GetState_ShouldProject_RuntimeFieldState_ToTextBoxState()
        {
            var definition = new YamlStringFieldDefinition
            {
                Id = "Name",
                CaptionKey = "person.name",
                PlaceholderKey = "person.name.placeholder",
                IsRequired = true,
                Width = 320,
                MaxLength = 50,
            };
            var resolved = new ResolvedFieldDefinition(
                definition,
                width: definition.Width,
                widthHint: null,
                visible: true,
                enabled: false,
                showOldValueRestoreButton: true,
                validationTrigger: ValidationTrigger.OnBlur,
                interactionMode: InteractionMode.Classic,
                densityMode: DensityMode.Compact,
                fieldChromeMode: FieldChromeMode.InlineHint,
                captionPlacement: CaptionPlacement.Top);
            var runtimeField = new RuntimeFieldState(resolved);
            runtimeField.LoadValue("Andrzej");
            runtimeField.SetValidation("required", "This field is required.");

            using (var binding = new YamlTextBoxFieldBinding(runtimeField))
            {
                var state = binding.GetState();

                Assert.Multiple(() =>
                {
                    Assert.That(state.FieldId, Is.EqualTo("Name"));
                    Assert.That(state.Caption, Is.EqualTo("person.name"));
                    Assert.That(state.Placeholder, Is.EqualTo("person.name.placeholder"));
                    Assert.That(state.Text, Is.EqualTo("Andrzej"));
                    Assert.That(state.OldValue, Is.EqualTo("Andrzej"));
                    Assert.That(state.ErrorMessage, Is.EqualTo("This field is required."));
                    Assert.That(state.IsRequired, Is.True);
                    Assert.That(state.IsEnabled, Is.False);
                    Assert.That(state.ShowOldValueRestoreButton, Is.True);
                    Assert.That(state.FieldChromeMode, Is.EqualTo(FieldChromeMode.InlineHint));
                    Assert.That(state.InteractionMode, Is.EqualTo(InteractionMode.Classic));
                    Assert.That(state.Width, Is.EqualTo(320d));
                    Assert.That(state.MaxLength, Is.EqualTo(50));
                });
            }
        }

        [Test]
        public void UpdateText_ShouldWriteBack_ToRuntimeFieldState()
        {
            var definition = new YamlStringFieldDefinition
            {
                Id = "Name",
            };
            var resolved = new ResolvedFieldDefinition(
                definition,
                width: null,
                widthHint: null,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: false,
                validationTrigger: ValidationTrigger.OnBlur,
                interactionMode: InteractionMode.Classic,
                densityMode: DensityMode.Compact,
                fieldChromeMode: FieldChromeMode.Framed,
                captionPlacement: CaptionPlacement.Top);
            var runtimeField = new RuntimeFieldState(resolved);

            using (var binding = new YamlTextBoxFieldBinding(runtimeField))
            {
                binding.UpdateText("Roland");

                Assert.Multiple(() =>
                {
                    Assert.That(runtimeField.Value, Is.EqualTo("Roland"));
                    Assert.That(runtimeField.IsDirty, Is.True);
                    Assert.That(runtimeField.IsTouched, Is.True);
                });
            }
        }

        [Test]
        public void RestoreOldValue_ShouldRestoreOriginalLoadedValue()
        {
            var definition = new YamlStringFieldDefinition
            {
                Id = "Name",
            };
            var resolved = new ResolvedFieldDefinition(
                definition,
                width: null,
                widthHint: null,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: true,
                validationTrigger: ValidationTrigger.OnBlur,
                interactionMode: InteractionMode.Classic,
                densityMode: DensityMode.Compact,
                fieldChromeMode: FieldChromeMode.Framed,
                captionPlacement: CaptionPlacement.Top);
            var runtimeField = new RuntimeFieldState(resolved);
            runtimeField.LoadValue("Roland");

            using (var binding = new YamlTextBoxFieldBinding(runtimeField))
            {
                binding.UpdateText(string.Empty);
                binding.RestoreOldValue();

                Assert.Multiple(() =>
                {
                    Assert.That(runtimeField.Value, Is.EqualTo("Roland"));
                    Assert.That(runtimeField.OldValue, Is.EqualTo("Roland"));
                    Assert.That(runtimeField.IsDirty, Is.False);
                });
            }
        }

        [Test]
        public void GetState_ShouldProject_WidthHint_ToBindingState()
        {
            var definition = new YamlStringFieldDefinition
            {
                Id = "Name",
                WidthHint = FieldWidthHint.Long,
            };
            var resolved = new ResolvedFieldDefinition(
                definition,
                width: null,
                widthHint: FieldWidthHint.Long,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: false,
                validationTrigger: ValidationTrigger.OnBlur,
                interactionMode: InteractionMode.Classic,
                densityMode: DensityMode.Compact,
                fieldChromeMode: FieldChromeMode.Framed,
                captionPlacement: CaptionPlacement.Top);
            var runtimeField = new RuntimeFieldState(resolved);

            using (var binding = new YamlTextBoxFieldBinding(runtimeField))
            {
                var state = binding.GetState();

                Assert.Multiple(() =>
                {
                    Assert.That(state.Width, Is.Null);
                    Assert.That(state.WidthHint, Is.EqualTo(FieldWidthHint.Long));
                });
            }
        }

        [Test]
        public void GetState_ShouldProject_InteractionMode_ToBindingState()
        {
            var definition = new YamlStringFieldDefinition
            {
                Id = "Name",
            };
            var resolved = new ResolvedFieldDefinition(
                definition,
                width: null,
                widthHint: null,
                visible: true,
                enabled: true,
                showOldValueRestoreButton: false,
                validationTrigger: ValidationTrigger.OnBlur,
                interactionMode: InteractionMode.Touch,
                densityMode: DensityMode.Normal,
                fieldChromeMode: FieldChromeMode.Framed,
                captionPlacement: CaptionPlacement.Top);
            var runtimeField = new RuntimeFieldState(resolved);

            using (var binding = new YamlTextBoxFieldBinding(runtimeField))
            {
                var state = binding.GetState();
                Assert.That(state.InteractionMode, Is.EqualTo(InteractionMode.Touch));
            }
        }
    }
}

