using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Commit;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionParticipant
    {
        private readonly Dictionary<string, EditSessionCellState> _cells =
            new Dictionary<string, EditSessionCellState>(StringComparer.Ordinal);

        public EditSessionParticipant(string targetId, string targetPath = null)
        {
            TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
            TargetPath = targetPath ?? string.Empty;
            EditState = RecordEditState.Unchanged;
            ValidationState = RecordValidationState.Unknown;
            AccessState = RecordAccessState.Editable;
            CommitState = RecordCommitState.Idle;
            CommitDetail = RecordCommitDetail.None;
            RecordValidationErrors = Array.Empty<GridValidationError>();
        }

        public string TargetId { get; }

        public string TargetPath { get; }

        public string VersionToken { get; internal set; } = string.Empty;

        public RecordEditState EditState { get; internal set; }

        public RecordValidationState ValidationState { get; internal set; }

        public RecordAccessState AccessState { get; internal set; }

        public RecordCommitState CommitState { get; internal set; }

        public RecordCommitDetail CommitDetail { get; internal set; }

        public RecordEditState? EditStateBeforeEditing { get; internal set; }

        public IReadOnlyList<GridValidationError> RecordValidationErrors { get; internal set; }

        public IReadOnlyDictionary<string, EditSessionCellState> Cells => _cells;

        public bool IsDirty =>
            EditState == RecordEditState.Modified ||
            EditState == RecordEditState.New ||
            EditState == RecordEditState.MarkedForDelete ||
            _cells.Values.Any(cell => cell.IsDirty);

        internal EditSessionCellState GetOrAddCell(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name is required.", nameof(fieldName));
            }

            EditSessionCellState state;
            if (!_cells.TryGetValue(fieldName, out state))
            {
                state = new EditSessionCellState(fieldName);
                _cells[fieldName] = state;
            }

            return state;
        }
    }
}
