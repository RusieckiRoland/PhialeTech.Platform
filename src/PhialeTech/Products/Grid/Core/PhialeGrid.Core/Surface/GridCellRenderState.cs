using System;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Surface
{
    public sealed class GridCellRenderState
    {
        public GridCellRenderState(
            string recordKey,
            string columnKey,
            CellDisplayState displayState,
            CellChangeState changeState,
            CellValidationState validationState,
            CellAccessState accessState,
            string sessionId = null)
        {
            RecordKey = recordKey ?? throw new ArgumentNullException(nameof(recordKey));
            ColumnKey = columnKey ?? throw new ArgumentNullException(nameof(columnKey));
            DisplayState = displayState;
            ChangeState = changeState;
            ValidationState = validationState;
            AccessState = accessState;
            SessionId = sessionId ?? string.Empty;
        }

        public string RecordKey { get; }

        public string ColumnKey { get; }

        public CellDisplayState DisplayState { get; }

        public CellChangeState ChangeState { get; }

        public CellValidationState ValidationState { get; }

        public CellAccessState AccessState { get; }

        public string SessionId { get; }
    }
}
