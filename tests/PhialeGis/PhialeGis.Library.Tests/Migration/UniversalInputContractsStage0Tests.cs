using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using UniversalInput.Contracts;
using UniversalInput.Contracts.EditorEnums;
using UniversalInput.Contracts.EventEnums;

namespace PhialeGis.Library.Tests.Migration
{
    [TestFixture]
    [Category("Unit")]
    [Category("MigrationGuard")]
    public sealed class UniversalInputContractsStage0Tests
    {
        [Test]
        public void Stage0_AssemblyContainsExpectedContractTypes()
        {
            var assembly = typeof(UniversalPoint).Assembly;
            var names = assembly.GetTypes().Select(t => t.FullName).ToArray();

            Assert.Contains("UniversalInput.Contracts.IUniversalBase", names);
            Assert.Contains("UniversalInput.Contracts.UniversalPoint", names);
            Assert.Contains("UniversalInput.Contracts.UniversalVector", names);
            Assert.Contains("UniversalInput.Contracts.UniversalPointer", names);
            Assert.Contains("UniversalInput.Contracts.UniversalMetadata", names);
            Assert.Contains("UniversalInput.Contracts.UniversalPointerRoutedEventArgs", names);
            Assert.Contains("UniversalInput.Contracts.UniversalManipulationDeltaRoutedEventArgs", names);
            Assert.Contains("UniversalInput.Contracts.UniversalTextChangedEventArgs", names);
            Assert.Contains("UniversalInput.Contracts.EditorEnums.EditorDiagnosticSeverity", names);
            Assert.Contains("UniversalInput.Contracts.EventEnums.UniversalManipulationModes", names);
        }

        [Test]
        public void Stage0_DtosCanBeSerializedWithSystemTextJson()
        {
            var point = new UniversalPoint { X = 10.5, Y = 20.25 };
            var metadata = new UniversalMetadata { ResetManipulationMode = true };
            var options = new JsonSerializerOptions { IncludeFields = true };

            var pointJson = JsonSerializer.Serialize(point, options);
            var metadataJson = JsonSerializer.Serialize(metadata, options);

            var pointRoundtrip = JsonSerializer.Deserialize<UniversalPoint>(pointJson, options);
            var metadataRoundtrip = JsonSerializer.Deserialize<UniversalMetadata>(metadataJson, options);

            Assert.AreEqual(10.5, pointRoundtrip.X);
            Assert.AreEqual(20.25, pointRoundtrip.Y);
            Assert.NotNull(metadataRoundtrip);
            Assert.IsTrue(metadataRoundtrip.ResetManipulationMode);
        }

        [Test]
        public void Stage0_EnumValuesRemainStable()
        {
            Assert.AreEqual(3, (int)EditorDiagnosticSeverity.Error);
            Assert.AreEqual(0x10u, (uint)UniversalManipulationModes.Rotate);
            Assert.AreEqual(0x20u, (uint)UniversalManipulationModes.Scale);
        }
    }
}

