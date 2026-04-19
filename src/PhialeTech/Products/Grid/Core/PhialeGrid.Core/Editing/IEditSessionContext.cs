using System;
using System.Collections.Generic;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.Editing
{
    public interface IEditSessionContext : IDisposable
    {
        event EventHandler StateChanged;

        IReadOnlyList<object> Records { get; }

        string CurrentRecordId { get; }

        object CurrentRecord { get; }

        string EditingRecordId { get; }

        string ActiveEditingFieldId { get; }

        bool IsInEditMode { get; }

        EditSession CurrentSession { get; }

        IReadOnlyList<IEditSessionFieldDefinition> FieldDefinitions { get; }

        IReadOnlyCollection<string> EditedRecordIds { get; }

        IReadOnlyCollection<string> InvalidRecordIds { get; }

        int PendingEditCount { get; }

        int ValidationIssueCount { get; }

        GridSurfaceStateProjection SurfaceStateProjection { get; }

        ChangeSetCommitState CommitState { get; }

        bool HasPendingEdits { get; }

        bool HasValidationIssues { get; }

        bool SetCurrentRecord(string recordId);

        bool ClearCurrentRecord();

        void Refresh();

        bool HasRecordChanges(string targetId);

        IReadOnlyList<EditSessionFieldChange> GetFieldChanges(string targetId);

        IReadOnlyList<EditSessionValidationDetail> GetValidationDetails(string targetId);

        IReadOnlyList<GridValidationError> ValidateFieldValue(string targetId, string fieldId, object value, string editingText = null);

        bool TrySetFieldValue(string targetId, string fieldId, object value, string editingText = null);

        bool BeginFieldEdit(string targetId, string fieldId, string targetPath = null);

        bool PostActiveEdit();

        bool CancelActiveEdit();

        bool CommitPendingChanges();

        void CancelPendingChanges();

        EditSession StartSession(EditSessionScopeKind scopeKind, SaveMode saveMode, string rootTargetId = null);

        void ClearSession();

        EditSessionParticipant EnsureParticipant(string targetId, string targetPath = null);

        EditSessionParticipant BeginRecordEdit(string targetId, string targetPath = null);

        EditSessionParticipant CompleteRecordEdit(string targetId, bool hasEffectiveChange);

        EditSessionParticipant CancelRecordEdit(string targetId);

        EditSessionParticipant MarkRecordAsNew(string targetId, string targetPath = null);

        EditSessionParticipant MarkRecordModified(string targetId, string targetPath = null);

        EditSessionParticipant MarkRecordForDelete(string targetId, string targetPath = null);

        EditSessionParticipant ClearRecordChanges(string targetId);

        EditSessionParticipant ApplyValidationErrors(
            string targetId,
            IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField,
            bool wasValidated = true);

        EditSessionParticipant SetCellDisplayState(string targetId, string fieldName, CellDisplayState displayState);

        EditSessionParticipant MarkCellModified(string targetId, string fieldName);

        EditSessionParticipant MarkCellUnchanged(string targetId, string fieldName);

        EditSessionParticipant SetCellAccessState(string targetId, string fieldName, CellAccessState accessState);

        ChangeSetCommitState StartCommit(ChangeSet changeSet);

        ChangeSetCommitState ApplyCommitOutcome(ChangeSetCommitOutcome outcome);
    }

    public interface IEditSessionContext<TRecord> : IEditSessionContext
    {
        event EventHandler<CurrentRecordChangedEventArgs<TRecord>> CurrentRecordChanged;

        new IReadOnlyList<TRecord> Records { get; }

        new TRecord CurrentRecord { get; }

        bool SetCurrentRecord(TRecord record);
    }
}
