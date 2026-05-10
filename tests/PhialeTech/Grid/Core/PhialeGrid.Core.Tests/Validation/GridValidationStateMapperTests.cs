using NUnit.Framework;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Tests.Validation
{
    [TestFixture]
    public class GridValidationStateMapperTests
    {
        [Test]
        public void NoValidationRun_MapsToUnknown()
        {
            Assert.Multiple(() =>
            {
                Assert.That(GridValidationStateMapper.ToRecordState(null, wasValidated: false), Is.EqualTo(RecordValidationState.Unknown));
                Assert.That(GridValidationStateMapper.ToCellState(null, wasValidated: false), Is.EqualTo(CellValidationState.Unknown));
            });
        }

        [Test]
        public void ErrorSeverity_MapsToInvalid()
        {
            var errors = new[]
            {
                new GridValidationError("Owner", "Required"),
            };

            Assert.Multiple(() =>
            {
                Assert.That(GridValidationStateMapper.ToRecordState(errors), Is.EqualTo(RecordValidationState.Invalid));
                Assert.That(GridValidationStateMapper.ToCellState(errors), Is.EqualTo(CellValidationState.Invalid));
            });
        }

        [Test]
        public void WarningOnly_MapsToWarning()
        {
            var warnings = new[]
            {
                new GridValidationError("Owner", "Suspicious value", severity: GridValidationSeverity.Warning),
            };

            Assert.Multiple(() =>
            {
                Assert.That(GridValidationStateMapper.ToRecordState(warnings), Is.EqualTo(RecordValidationState.Warning));
                Assert.That(GridValidationStateMapper.ToCellState(warnings), Is.EqualTo(CellValidationState.Warning));
            });
        }
    }
}

