using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionCellState
    {
        public EditSessionCellState(string fieldName)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            DisplayState = CellDisplayState.Normal;
            ChangeState = CellChangeState.Unchanged;
            ValidationState = CellValidationState.Unknown;
            AccessState = CellAccessState.Editable;
            ValidationErrors = Array.Empty<GridValidationError>();
        }

        public string FieldName { get; }

        public CellDisplayState DisplayState { get; internal set; }

        public CellChangeState ChangeState { get; internal set; }

        public CellValidationState ValidationState { get; internal set; }

        public CellAccessState AccessState { get; internal set; }

        public IReadOnlyList<GridValidationError> ValidationErrors { get; internal set; }

        public bool IsDirty => ChangeState == CellChangeState.Modified;
    }
}
