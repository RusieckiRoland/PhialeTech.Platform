using System;
using System.Linq;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;
using UniversalInput.Contracts.EditorEnums;
using UniversalInput.Contracts.EventEnums;

namespace PhialeGis.Library.Tests.Migration
{
    [TestFixture]
    [Category("Unit")]
    [Category("MigrationGuard")]
    public sealed class UniversalContractsMigrationTests
    {
        [Test]
        public void RequiredUniversalContracts_ArePresentWithExpectedKinds()
        {
            var assembly = typeof(UniversalPoint).Assembly;

            var requiredTypes = new[]
            {
                "UniversalInput.Contracts.UniversalPoint",
                "UniversalInput.Contracts.UniversalVector",
                "UniversalInput.Contracts.UniversalPointer",
                "UniversalInput.Contracts.UniversalMetadata",
                "UniversalInput.Contracts.DeviceType",
                "UniversalInput.Contracts.IUniversalBase",
                "UniversalInput.Contracts.UniversalPointerRoutedEventArgs",
                "UniversalInput.Contracts.UniversalManipulationStartingRoutedEventArgs",
                "UniversalInput.Contracts.UniversalManipulationStartedRoutedEventArgs",
                "UniversalInput.Contracts.UniversalManipulationDeltaRoutedEventArgs",
                "UniversalInput.Contracts.UniversalManipulationCompletedRoutedEventArgs",
                "UniversalInput.Contracts.UniversalTextChangedEventArgs",
                "UniversalInput.Contracts.UniversalSelectionChangedEventArgs",
                "UniversalInput.Contracts.UniversalCaretMovedEventArgs",
                "UniversalInput.Contracts.UniversalDirtyChangedEventArgs",
                "UniversalInput.Contracts.UniversalCommandEventArgs",
                "UniversalInput.Contracts.UniversalSaveRequestedEventArgs",
                "UniversalInput.Contracts.UniversalLanguageChangedEventArgs",
                "UniversalInput.Contracts.UniversalThemeChangedEventArgs",
                "UniversalInput.Contracts.UniversalFindRequestedEventArgs",
                "UniversalInput.Contracts.UniversalReplaceRequestedEventArgs",
                "UniversalInput.Contracts.UniversalLinkClickedEventArgs",
                "UniversalInput.Contracts.UniversalDiagnosticsUpdatedEventArgs",
                "UniversalInput.Contracts.UniversalHoverRequestedEventArgs",
                "UniversalInput.Contracts.EditorEnums.EditorDiagnosticSeverity",
                "UniversalInput.Contracts.EventEnums.UniversalManipulationModes"
            };

            var missing = requiredTypes
                .Where(typeName => assembly.GetType(typeName) == null)
                .ToArray();

            Assert.IsEmpty(missing, "Missing types: " + string.Join(", ", missing));
            Assert.IsTrue(typeof(UniversalPoint).IsValueType);
            Assert.IsTrue(typeof(UniversalVector).IsValueType);
            Assert.IsTrue(typeof(DeviceType).IsEnum);
            Assert.IsTrue(typeof(EditorDiagnosticSeverity).IsEnum);
            Assert.IsTrue(typeof(UniversalManipulationModes).IsEnum);
        }

        [Test]
        public void EventArgs_NullInputFallbacksRemainStable()
        {
            var command = new UniversalCommandEventArgs(null, ctrl: true, alt: false, shift: true);
            var text = new UniversalTextChangedEventArgs(null);
            var save = new UniversalSaveRequestedEventArgs(null);
            var language = new UniversalLanguageChangedEventArgs(null);
            var theme = new UniversalThemeChangedEventArgs(null);
            var find = new UniversalFindRequestedEventArgs(null, false, false, false);
            var replace = new UniversalReplaceRequestedEventArgs(null, null, false, false, false);
            var link = new UniversalLinkClickedEventArgs(null);
            var diagnostics = new UniversalDiagnosticsUpdatedEventArgs(
                null,
                null,
                null,
                null,
                null,
                null);

            Assert.AreEqual(string.Empty, command.CommandId);
            Assert.AreEqual(string.Empty, text.Text);
            Assert.AreEqual(string.Empty, save.Reason);
            Assert.AreEqual("plaintext", language.LanguageId);
            Assert.AreEqual("default", theme.ThemeId);
            Assert.AreEqual(string.Empty, find.Query);
            Assert.AreEqual(string.Empty, replace.Query);
            Assert.AreEqual(string.Empty, replace.Replacement);
            Assert.AreEqual(string.Empty, link.Url);

            Assert.AreEqual(string.Empty, diagnostics.DocumentId);
            Assert.NotNull(diagnostics.Lines);
            Assert.NotNull(diagnostics.Columns);
            Assert.NotNull(diagnostics.Lengths);
            Assert.NotNull(diagnostics.Severities);
            Assert.NotNull(diagnostics.Messages);
            Assert.AreEqual(0, diagnostics.Lines.Length);
            Assert.AreEqual(0, diagnostics.Messages.Length);
        }

        [Test]
        public void UniversalPointer_StoresDeviceAndPositions()
        {
            var point = new UniversalPoint { X = 123.5, Y = 987.25 };
            var pointer = new UniversalPointer(DeviceType.Mouse, point)
            {
                Properties = new UniversalPointerPointProperties { IsLeftButtonPressed = true }
            };

            Assert.AreEqual(DeviceType.Mouse, pointer.PointerDeviceType);
            Assert.AreEqual(123.5, pointer.Position.X);
            Assert.AreEqual(987.25, pointer.Position.Y);
            Assert.IsTrue(pointer.Properties.IsLeftButtonPressed);
        }

        [Test]
        public void AbstractionsAssembly_DoesNotContainLocalUniversalEventDefinitions()
        {
            var abstractionsAssembly = typeof(IGisInteractionManager).Assembly;
            var oldType = abstractionsAssembly.GetType("PhialeGis.Library.Abstractions.UniversalEvents.UniversalPoint");

            Assert.IsNull(oldType, "Abstractions should consume contracts from UniversalInput.Contracts instead of local duplicates.");
        }
    }
}

