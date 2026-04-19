using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionStateMachine
    {
        public EditSessionParticipant BeginRecordEdit(EditSession session, string targetId, string targetPath = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var participant = session.GetOrAddParticipant(targetId, targetPath);
            if (participant.AccessState != RecordAccessState.Editable)
            {
                throw new InvalidOperationException("Record is not editable.");
            }

            if (participant.EditState == RecordEditState.MarkedForDelete)
            {
                throw new InvalidOperationException("Cannot edit a record marked for delete.");
            }

            if (participant.EditState != RecordEditState.Editing)
            {
                participant.EditStateBeforeEditing = participant.EditState;
                participant.EditState = RecordEditState.Editing;
            }

            return participant;
        }

        public EditSessionParticipant MarkRecordAsNew(EditSession session, string targetId, string targetPath = null)
        {
            var participant = RequireSession(session).GetOrAddParticipant(targetId, targetPath);
            participant.EditState = RecordEditState.New;
            participant.EditStateBeforeEditing = null;
            participant.CommitState = RecordCommitState.Idle;
            participant.CommitDetail = RecordCommitDetail.None;
            return participant;
        }

        public EditSessionParticipant MarkRecordForDelete(EditSession session, string targetId, string targetPath = null)
        {
            var participant = RequireSession(session).GetOrAddParticipant(targetId, targetPath);
            participant.EditState = RecordEditState.MarkedForDelete;
            participant.EditStateBeforeEditing = null;
            return participant;
        }

        public EditSessionParticipant CompleteRecordEdit(EditSession session, string targetId, bool hasEffectiveChange)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            if (participant.EditState != RecordEditState.Editing)
            {
                return participant;
            }

            var priorState = participant.EditStateBeforeEditing ?? RecordEditState.Unchanged;
            switch (priorState)
            {
                case RecordEditState.New:
                    participant.EditState = RecordEditState.New;
                    break;
                case RecordEditState.Modified:
                    participant.EditState = RecordEditState.Modified;
                    break;
                case RecordEditState.Unchanged:
                    participant.EditState = hasEffectiveChange
                        ? RecordEditState.Modified
                        : RecordEditState.Unchanged;
                    break;
                default:
                    participant.EditState = priorState;
                    break;
            }

            participant.EditStateBeforeEditing = null;
            return participant;
        }

        public EditSessionParticipant CancelRecordEdit(EditSession session, string targetId)
        {
            return CompleteRecordEdit(session, targetId, hasEffectiveChange: false);
        }

        public EditSessionParticipant ApplyRecordValidation(
            EditSession session,
            string targetId,
            RecordValidationState validationState,
            IReadOnlyDictionary<string, CellValidationState> cellStates = null)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.ValidationState = validationState;

            if (cellStates != null)
            {
                foreach (var item in cellStates)
                {
                    participant.GetOrAddCell(item.Key).ValidationState = item.Value;
                }
            }

            return participant;
        }

        public EditSessionParticipant ApplyValidationErrors(
            EditSession session,
            string targetId,
            IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField,
            bool wasValidated = true)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            var mappedCellStates = new Dictionary<string, CellValidationState>(StringComparer.Ordinal);
            var flattenedErrors = new List<GridValidationError>();
            participant.RecordValidationErrors = Array.Empty<GridValidationError>();

            foreach (var existingCell in participant.Cells.Values)
            {
                existingCell.ValidationErrors = Array.Empty<GridValidationError>();
                existingCell.ValidationState = wasValidated ? CellValidationState.Valid : CellValidationState.Unknown;
            }

            if (cellErrorsByField != null)
            {
                foreach (var item in cellErrorsByField)
                {
                    var errors = (item.Value ?? Array.Empty<GridValidationError>()).ToArray();
                    flattenedErrors.AddRange(errors);
                    if (string.IsNullOrWhiteSpace(item.Key))
                    {
                        participant.RecordValidationErrors = errors;
                        continue;
                    }

                    mappedCellStates[item.Key] = GridValidationStateMapper.ToCellState(errors, wasValidated);
                    var cell = participant.GetOrAddCell(item.Key);
                    cell.ValidationErrors = errors;
                }
            }

            return ApplyRecordValidation(
                session,
                targetId,
                GridValidationStateMapper.ToRecordState(flattenedErrors, wasValidated),
                mappedCellStates);
        }

        public EditSessionParticipant SetCellDisplayState(
            EditSession session,
            string targetId,
            string fieldName,
            CellDisplayState displayState)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.GetOrAddCell(fieldName).DisplayState = displayState;
            return participant;
        }

        public EditSessionParticipant MarkCellModified(EditSession session, string targetId, string fieldName)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.GetOrAddCell(fieldName).ChangeState = CellChangeState.Modified;
            return participant;
        }

        public EditSessionParticipant MarkCellUnchanged(EditSession session, string targetId, string fieldName)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.GetOrAddCell(fieldName).ChangeState = CellChangeState.Unchanged;
            return participant;
        }

        public EditSessionParticipant MarkRecordModified(EditSession session, string targetId, string targetPath = null)
        {
            var participant = RequireSession(session).GetOrAddParticipant(targetId, targetPath);
            if (participant.EditState != RecordEditState.New &&
                participant.EditState != RecordEditState.MarkedForDelete &&
                participant.EditState != RecordEditState.Editing)
            {
                participant.EditState = RecordEditState.Modified;
            }

            return participant;
        }

        public EditSessionParticipant ClearRecordChanges(EditSession session, string targetId)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.EditState = RecordEditState.Unchanged;
            participant.EditStateBeforeEditing = null;
            participant.CommitState = RecordCommitState.Idle;
            participant.CommitDetail = RecordCommitDetail.None;
            foreach (var cell in participant.Cells.Values)
            {
                cell.ChangeState = CellChangeState.Unchanged;
            }

            return participant;
        }

        public EditSessionParticipant SetCellAccessState(
            EditSession session,
            string targetId,
            string fieldName,
            CellAccessState accessState)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.GetOrAddCell(fieldName).AccessState = accessState;
            return participant;
        }

        public EditSessionParticipant ApplyCommitState(
            EditSession session,
            string targetId,
            RecordCommitState commitState,
            RecordCommitDetail commitDetail)
        {
            var participant = GetRequiredParticipant(RequireSession(session), targetId);
            participant.CommitState = commitState;
            participant.CommitDetail = commitDetail;
            return participant;
        }

        public void ApplySuccessfulCommit(EditSession session, bool removeDeletedParticipants = true)
        {
            var validatedSession = RequireSession(session);
            foreach (var participant in validatedSession.Participants.ToArray())
            {
                participant.CommitState = RecordCommitState.Committed;
                participant.CommitDetail = RecordCommitDetail.None;
                participant.EditStateBeforeEditing = null;

                foreach (var cell in participant.Cells.Values)
                {
                    cell.ChangeState = CellChangeState.Unchanged;
                }

                if (participant.EditState == RecordEditState.MarkedForDelete && removeDeletedParticipants)
                {
                    validatedSession.RemoveParticipant(participant.TargetId);
                    continue;
                }

                participant.EditState = RecordEditState.Unchanged;
            }
        }

        private static EditSession RequireSession(EditSession session)
        {
            return session ?? throw new ArgumentNullException(nameof(session));
        }

        private static EditSessionParticipant GetRequiredParticipant(EditSession session, string targetId)
        {
            EditSessionParticipant participant;
            if (!session.TryGetParticipant(targetId, out participant))
            {
                throw new InvalidOperationException("Session does not contain participant: " + targetId);
            }

            return participant;
        }
    }
}
