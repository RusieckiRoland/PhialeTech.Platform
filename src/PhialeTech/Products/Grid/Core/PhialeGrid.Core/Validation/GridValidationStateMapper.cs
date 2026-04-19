using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Validation
{
    public static class GridValidationStateMapper
    {
        public static RecordValidationState ToRecordState(
            IReadOnlyCollection<GridValidationError> errors,
            bool wasValidated = true)
        {
            if (!wasValidated)
            {
                return RecordValidationState.Unknown;
            }

            if (errors == null || errors.Count == 0)
            {
                return RecordValidationState.Valid;
            }

            return errors.All(error => error != null && error.Severity == GridValidationSeverity.Warning)
                ? RecordValidationState.Warning
                : RecordValidationState.Invalid;
        }

        public static CellValidationState ToCellState(
            IReadOnlyCollection<GridValidationError> errors,
            bool wasValidated = true)
        {
            if (!wasValidated)
            {
                return CellValidationState.Unknown;
            }

            if (errors == null || errors.Count == 0)
            {
                return CellValidationState.Valid;
            }

            return errors.All(error => error != null && error.Severity == GridValidationSeverity.Warning)
                ? CellValidationState.Warning
                : CellValidationState.Invalid;
        }
    }
}
