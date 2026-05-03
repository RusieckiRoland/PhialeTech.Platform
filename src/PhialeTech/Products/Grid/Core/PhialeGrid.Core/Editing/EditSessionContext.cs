using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Surface;
using PhialeGrid.Core.Validation;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSessionContext<TRecord> : IEditSessionContext<TRecord>, IDisposable
    {
        private readonly IEditSessionDataSource<TRecord> _dataSource;
        private readonly Func<TRecord, string> _recordIdSelector;
        private readonly EditSessionStateMachine _stateMachine;
        private readonly IChangeSetCommitCoordinator _commitCoordinator;
        private readonly GridFieldValueValidator _fieldValueValidator;
        private readonly Dictionary<string, TRecord> _recordsById = new Dictionary<string, TRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, object>> _baselineValuesByRecord = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, EditSessionFieldChange>> _changesByRecord = new Dictionary<string, Dictionary<string, EditSessionFieldChange>>(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<EditSessionValidationDetail>> _validationDetailsByRecord = new Dictionary<string, IReadOnlyList<EditSessionValidationDetail>>(StringComparer.Ordinal);
        private readonly HashSet<string> _editedRecordIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _invalidRecordIds = new HashSet<string>(StringComparer.Ordinal);
        private IReadOnlyList<TRecord> _records = Array.Empty<TRecord>();
        private IReadOnlyList<object> _recordObjects = Array.Empty<object>();
        private IReadOnlyList<IEditSessionFieldDefinition> _fieldDefinitions = Array.Empty<IEditSessionFieldDefinition>();
        private bool _disposed;
        private int _stateChangeBatchDepth;
        private bool _hasPendingStateChanged;
        private int _validationIssueCount;

        public EditSessionContext(
            IEditSessionDataSource<TRecord> dataSource,
            Func<TRecord, string> recordIdSelector,
            EditSessionStateMachine stateMachine = null,
            IChangeSetCommitCoordinator commitCoordinator = null,
            GridFieldValueValidator fieldValueValidator = null)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _recordIdSelector = recordIdSelector ?? throw new ArgumentNullException(nameof(recordIdSelector));
            _stateMachine = stateMachine ?? new EditSessionStateMachine();
            _commitCoordinator = commitCoordinator ?? new ChangeSetCommitCoordinator();
            _fieldValueValidator = fieldValueValidator ?? new GridFieldValueValidator();
            LoadSnapshotFromSource();
            _dataSource.VersionChanged += HandleDataSourceVersionChanged;
        }

        public event EventHandler<CurrentRecordChangedEventArgs<TRecord>> CurrentRecordChanged;

        public event EventHandler StateChanged;

        public IReadOnlyList<TRecord> Records => _records;

        public string CurrentRecordId { get; private set; } = string.Empty;

        public TRecord CurrentRecord { get; private set; }

        public string EditingRecordId => CurrentSession?.EditingRecordId ?? string.Empty;

        public string ActiveEditingFieldId => CurrentSession?.ActiveEditingFieldId ?? string.Empty;

        public bool IsInEditMode => !string.IsNullOrWhiteSpace(EditingRecordId) && !string.IsNullOrWhiteSpace(ActiveEditingFieldId);

        public EditSession CurrentSession { get; private set; }

        public IReadOnlyList<IEditSessionFieldDefinition> FieldDefinitions => _fieldDefinitions;

        public IReadOnlyCollection<string> EditedRecordIds => _editedRecordIds;

        public IReadOnlyCollection<string> InvalidRecordIds => _invalidRecordIds;

        public int PendingEditCount => _editedRecordIds.Count;

        public int ValidationIssueCount => _validationIssueCount;

        public GridSurfaceStateProjection SurfaceStateProjection { get; private set; } = GridSurfaceStateProjection.Empty;

        public ChangeSetCommitState CommitState => _commitCoordinator.CurrentState;

        public bool HasPendingEdits => _editedRecordIds.Count > 0;

        public bool HasValidationIssues => _invalidRecordIds.Count > 0;

        public IDisposable BeginStateChangeBatch(string reason)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Edit session state change batch reason is required.", nameof(reason));
            }

            _stateChangeBatchDepth++;
            return new StateChangeBatch(this);
        }

        public bool SetCurrentRecord(string recordId)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(recordId))
            {
                return ClearCurrentRecord();
            }

            if (!_recordsById.TryGetValue(recordId, out var record) ||
                string.Equals(CurrentRecordId, recordId, StringComparison.Ordinal))
            {
                return false;
            }

            var previousId = CurrentRecordId;
            var previousRecord = CurrentRecord;
            CurrentRecordId = recordId;
            CurrentRecord = record;
            RaiseCurrentRecordChanged(previousId, previousRecord, CurrentRecordId, CurrentRecord);
            return true;
        }

        public bool SetCurrentRecord(TRecord record)
        {
            ThrowIfDisposed();
            if (EqualityComparer<TRecord>.Default.Equals(record, default(TRecord)))
            {
                return ClearCurrentRecord();
            }

            var recordId = _recordIdSelector(record);
            if (string.IsNullOrWhiteSpace(recordId))
            {
                throw new InvalidOperationException("Record id selector returned an empty id.");
            }

            if (string.Equals(CurrentRecordId, recordId, StringComparison.Ordinal))
            {
                return false;
            }

            var previousId = CurrentRecordId;
            var previousRecord = CurrentRecord;
            CurrentRecordId = recordId;
            CurrentRecord = record;
            RaiseCurrentRecordChanged(previousId, previousRecord, CurrentRecordId, CurrentRecord);
            return true;
        }

        public bool ClearCurrentRecord()
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(CurrentRecordId) && EqualityComparer<TRecord>.Default.Equals(CurrentRecord, default(TRecord)))
            {
                return false;
            }

            var previousId = CurrentRecordId;
            var previousRecord = CurrentRecord;
            CurrentRecordId = string.Empty;
            CurrentRecord = default(TRecord);
            RaiseCurrentRecordChanged(previousId, previousRecord, CurrentRecordId, CurrentRecord);
            return true;
        }

        public void Refresh()
        {
            ThrowIfDisposed();
            LoadSnapshotFromSource();
            PruneTrackingForMissingRecords();
            if (!string.IsNullOrWhiteSpace(CurrentRecordId))
            {
                if (_recordsById.TryGetValue(CurrentRecordId, out var refreshed))
                {
                    CurrentRecord = refreshed;
                }
                else
                {
                    ClearCurrentRecord();
                }
            }

            UpdateSurfaceProjection();
            RaiseStateChanged();
        }

        public bool HasRecordChanges(string targetId)
        {
            return !string.IsNullOrWhiteSpace(targetId) &&
                _changesByRecord.TryGetValue(targetId, out var changes) &&
                changes.Count > 0;
        }

        public IReadOnlyList<EditSessionFieldChange> GetFieldChanges(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId) || !_changesByRecord.TryGetValue(targetId, out var changes))
            {
                return Array.Empty<EditSessionFieldChange>();
            }

            return changes.Values
                .OrderBy(change => ResolveFieldDisplayIndex(change.FieldId))
                .ThenBy(change => change.FieldId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public IReadOnlyList<EditSessionValidationDetail> GetValidationDetails(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return Array.Empty<EditSessionValidationDetail>();
            }

            return _validationDetailsByRecord.TryGetValue(targetId, out var details)
                ? details
                : Array.Empty<EditSessionValidationDetail>();
        }

        public IReadOnlyList<GridValidationError> ValidateFieldValue(string targetId, string fieldId, object value, string editingText = null)
        {
            ThrowIfDisposed();
            if (!_recordsById.TryGetValue(targetId ?? string.Empty, out var record))
            {
                return Array.Empty<GridValidationError>();
            }

            var fieldDefinition = ResolveFieldDefinition(fieldId);
            if (fieldDefinition == null)
            {
                return Array.Empty<GridValidationError>();
            }

            var candidate = _dataSource.CreateWorkingCopy(record);
            if (EqualityComparer<TRecord>.Default.Equals(candidate, default(TRecord)))
            {
                return Array.Empty<GridValidationError>();
            }

            try
            {
                fieldDefinition.SetValue(candidate, value);
            }
            catch (Exception ex)
            {
                return new[] { new GridValidationError(fieldId, ex.Message) };
            }

            var snapshot = BuildValidationSnapshot(candidate, fieldId, editingText);
            var hadTrackedValidation = _validationDetailsByRecord.ContainsKey(targetId) || _invalidRecordIds.Contains(targetId);
            if (snapshot.Details.Count > 0 || hadTrackedValidation)
            {
                var session = EnsureSessionForMutation(targetId);
                session.GetOrAddParticipant(targetId);
                var validationChanged = StoreValidationState(targetId, snapshot.CellErrorsByField, true, snapshot.Details);
                TryPruneParticipant(targetId);
                if (validationChanged)
                {
                    UpdateSurfaceProjection();
                    RaiseStateChanged();
                }
            }

            var result = new List<GridValidationError>();
            if (snapshot.CellErrorsByField.TryGetValue(fieldId, out var fieldErrors))
            {
                result.AddRange(fieldErrors);
            }

            if (snapshot.CellErrorsByField.TryGetValue(string.Empty, out var recordErrors))
            {
                result.AddRange(recordErrors);
            }

            return result.ToArray();
        }

        public bool TrySetFieldValue(string targetId, string fieldId, object value, string editingText = null)
        {
            ThrowIfDisposed();
            if (!_recordsById.TryGetValue(targetId ?? string.Empty, out var record))
            {
                return false;
            }

            var fieldDefinition = ResolveFieldDefinition(fieldId);
            if (fieldDefinition == null)
            {
                return false;
            }

            var session = EnsureSessionForMutation(targetId);
            var participant = session.GetOrAddParticipant(targetId);
            var originalValue = EnsureBaselineValue(targetId, fieldDefinition, record);
            fieldDefinition.SetValue(record, value);
            var currentValue = fieldDefinition.GetValue(record);
            var hasChange = !Equals(originalValue, currentValue);

            if (hasChange)
            {
                TrackFieldChange(targetId, fieldDefinition, originalValue, currentValue);
                _stateMachine.MarkCellModified(session, targetId, fieldId);
                if (participant.EditState != RecordEditState.Editing &&
                    participant.EditState != RecordEditState.New &&
                    participant.EditState != RecordEditState.MarkedForDelete)
                {
                    _stateMachine.MarkRecordModified(session, targetId);
                }
            }
            else
            {
                ClearFieldChange(targetId, fieldId);
                _stateMachine.MarkCellUnchanged(session, targetId, fieldId);
                if (participant.EditState != RecordEditState.Editing &&
                    participant.EditState != RecordEditState.New &&
                    participant.EditState != RecordEditState.MarkedForDelete &&
                    !HasRecordChanges(targetId))
                {
                    _stateMachine.ClearRecordChanges(session, targetId);
                }
            }

            ApplyRecordValidationInternal(targetId, record);
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return true;
        }

        public bool BeginFieldEdit(string targetId, string fieldId, string targetPath = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(targetId) || string.IsNullOrWhiteSpace(fieldId))
            {
                return false;
            }

            var session = EnsureSessionForMutation(targetId);
            if (string.Equals(session.EditingRecordId, targetId, StringComparison.Ordinal) &&
                string.Equals(session.ActiveEditingFieldId, fieldId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(session.EditingRecordId) &&
                !string.Equals(session.EditingRecordId, targetId, StringComparison.Ordinal))
            {
                ClearActiveFieldEditState(session, completeRecordEdit: true);
            }
            else
            {
                ClearActiveFieldDisplayState(session);
            }

            _stateMachine.BeginRecordEdit(session, targetId, targetPath);
            session.EditingRecordId = targetId;
            session.ActiveEditingFieldId = fieldId;
            _stateMachine.SetCellDisplayState(session, targetId, fieldId, CellDisplayState.Current);
            SetCurrentRecord(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return true;
        }

        public bool PostActiveEdit()
        {
            ThrowIfDisposed();
            if (CurrentSession == null || !IsInEditMode)
            {
                return false;
            }

            ClearActiveFieldEditState(CurrentSession, completeRecordEdit: true);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return true;
        }

        public bool CancelActiveEdit()
        {
            ThrowIfDisposed();
            if (CurrentSession == null || !IsInEditMode)
            {
                return false;
            }

            var targetId = EditingRecordId;
            ClearActiveFieldDisplayState(CurrentSession);
            _stateMachine.CancelRecordEdit(CurrentSession, targetId);
            CurrentSession.EditingRecordId = string.Empty;
            CurrentSession.ActiveEditingFieldId = string.Empty;
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return true;
        }

        public bool CommitPendingChanges()
        {
            ThrowIfDisposed();
            if (IsInEditMode)
            {
                PostActiveEdit();
            }

            if (!HasPendingEdits)
            {
                return true;
            }

            foreach (var recordId in _editedRecordIds.ToArray())
            {
                if (_recordsById.TryGetValue(recordId, out var record))
                {
                    ApplyRecordValidationInternal(recordId, record);
                }
            }

            UpdateSurfaceProjection();
            RaiseStateChanged();
            if (HasValidationIssues)
            {
                return false;
            }

            var session = EnsureSessionForMutation(CurrentRecordId);
            var changeSet = BuildChangeSet(session);
            var startState = _commitCoordinator.Start(session, changeSet);
            ApplyCommitStateToAllParticipants(startState.CommitState, startState.CommitDetail);
            if (session.SaveMode == SaveMode.Direct)
            {
                ApplyCommitOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.DirectSucceeded));
            }

            return true;
        }

        public void CancelPendingChanges()
        {
            ThrowIfDisposed();
            if (CurrentSession != null)
            {
                ClearActiveFieldDisplayState(CurrentSession);
                CurrentSession.EditingRecordId = string.Empty;
                CurrentSession.ActiveEditingFieldId = string.Empty;
            }

            foreach (var recordEntry in _baselineValuesByRecord.ToArray())
            {
                if (!_recordsById.TryGetValue(recordEntry.Key, out var record))
                {
                    continue;
                }

                foreach (var fieldEntry in recordEntry.Value)
                {
                    var fieldDefinition = ResolveFieldDefinition(fieldEntry.Key);
                    if (fieldDefinition != null)
                    {
                        fieldDefinition.SetValue(record, fieldEntry.Value);
                    }
                }

                if (CurrentSession != null && CurrentSession.TryGetParticipant(recordEntry.Key, out _))
                {
                    _stateMachine.ClearRecordChanges(CurrentSession, recordEntry.Key);
                    _stateMachine.ApplyValidationErrors(CurrentSession, recordEntry.Key, null, wasValidated: false);
                    TryPruneParticipant(recordEntry.Key);
                }
            }

            _baselineValuesByRecord.Clear();
            _changesByRecord.Clear();
            _validationDetailsByRecord.Clear();
            _editedRecordIds.Clear();
            _invalidRecordIds.Clear();
            _validationIssueCount = 0;
            UpdateSurfaceProjection();
            RaiseStateChanged();
        }

        public EditSession StartSession(EditSessionScopeKind scopeKind, SaveMode saveMode, string rootTargetId = null)
        {
            ThrowIfDisposed();
            var resolvedRootId = string.IsNullOrWhiteSpace(rootTargetId) ? CurrentRecordId : rootTargetId;
            if (string.IsNullOrWhiteSpace(resolvedRootId))
            {
                throw new InvalidOperationException("Cannot start an edit session without a root target id.");
            }

            if (CurrentSession != null &&
                string.Equals(CurrentSession.RootTargetId, resolvedRootId, StringComparison.Ordinal) &&
                CurrentSession.ScopeKind == scopeKind &&
                CurrentSession.SaveMode == saveMode)
            {
                return CurrentSession;
            }

            CurrentSession = new EditSession(Guid.NewGuid().ToString("N"), scopeKind, resolvedRootId, saveMode);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return CurrentSession;
        }

        public void ClearSession()
        {
            ThrowIfDisposed();
            CurrentSession = null;
            SurfaceStateProjection = GridSurfaceStateProjection.Empty;
            RaiseStateChanged();
        }

        public EditSessionParticipant EnsureParticipant(string targetId, string targetPath = null)
        {
            var participant = EnsureSessionForMutation(targetId).GetOrAddParticipant(targetId, targetPath);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant BeginRecordEdit(string targetId, string targetPath = null)
        {
            var participant = _stateMachine.BeginRecordEdit(EnsureSessionForMutation(targetId), targetId, targetPath);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant CompleteRecordEdit(string targetId, bool hasEffectiveChange)
        {
            var participant = _stateMachine.CompleteRecordEdit(RequireSession(), targetId, hasEffectiveChange);
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant CancelRecordEdit(string targetId)
        {
            var participant = _stateMachine.CancelRecordEdit(RequireSession(), targetId);
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant MarkRecordAsNew(string targetId, string targetPath = null)
        {
            var participant = _stateMachine.MarkRecordAsNew(EnsureSessionForMutation(targetId), targetId, targetPath);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant MarkRecordModified(string targetId, string targetPath = null)
        {
            var participant = _stateMachine.MarkRecordModified(EnsureSessionForMutation(targetId), targetId, targetPath);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant MarkRecordForDelete(string targetId, string targetPath = null)
        {
            var participant = _stateMachine.MarkRecordForDelete(EnsureSessionForMutation(targetId), targetId, targetPath);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant ClearRecordChanges(string targetId)
        {
            ThrowIfDisposed();
            if (CurrentSession == null || !CurrentSession.TryGetParticipant(targetId, out _))
            {
                return null;
            }

            ClearTrackedRecordState(targetId);
            var participant = _stateMachine.ClearRecordChanges(CurrentSession, targetId);
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant ApplyValidationErrors(string targetId, IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField, bool wasValidated = true)
        {
            var session = EnsureSessionForMutation(targetId);
            session.GetOrAddParticipant(targetId);
            var participant = _stateMachine.ApplyValidationErrors(session, targetId, cellErrorsByField, wasValidated);
            StoreValidationState(targetId, cellErrorsByField, wasValidated);
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant SetCellDisplayState(string targetId, string fieldName, CellDisplayState displayState)
        {
            var participant = _stateMachine.SetCellDisplayState(EnsureSessionForMutation(targetId), targetId, fieldName, displayState);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant MarkCellModified(string targetId, string fieldName)
        {
            var participant = _stateMachine.MarkCellModified(EnsureSessionForMutation(targetId), targetId, fieldName);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant MarkCellUnchanged(string targetId, string fieldName)
        {
            var participant = _stateMachine.MarkCellUnchanged(EnsureSessionForMutation(targetId), targetId, fieldName);
            TryPruneParticipant(targetId);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public EditSessionParticipant SetCellAccessState(string targetId, string fieldName, CellAccessState accessState)
        {
            var participant = _stateMachine.SetCellAccessState(EnsureSessionForMutation(targetId), targetId, fieldName, accessState);
            UpdateSurfaceProjection();
            RaiseStateChanged();
            return participant;
        }

        public ChangeSetCommitState StartCommit(ChangeSet changeSet)
        {
            var session = RequireSession();
            var state = _commitCoordinator.Start(session, changeSet);
            ApplyCommitStateToAllParticipants(state.CommitState, state.CommitDetail);
            return state;
        }

        public ChangeSetCommitState ApplyCommitOutcome(ChangeSetCommitOutcome outcome)
        {
            var state = _commitCoordinator.ApplyOutcome(outcome);
            ApplyCommitStateToAllParticipants(state.CommitState, state.CommitDetail);
            if (state.CommitState == RecordCommitState.Committed)
            {
                ClearActiveFieldEditState(RequireSession(), completeRecordEdit: false, clearParticipantDisplayOnly: true);
                _stateMachine.ApplySuccessfulCommit(RequireSession());
                ClearCommittedChangeTracking();
                UpdateSurfaceProjection();
                RaiseStateChanged();
            }

            return state;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _dataSource.VersionChanged -= HandleDataSourceVersionChanged;
            _disposed = true;
        }

        private void LoadSnapshotFromSource()
        {
            _records = _dataSource.GetSnapshot() ?? Array.Empty<TRecord>();
            _recordObjects = _records.Cast<object>().ToArray();
            _fieldDefinitions = _dataSource.GetFieldDefinitions() ?? Array.Empty<IEditSessionFieldDefinition>();
            _recordsById.Clear();
            foreach (var record in _records)
            {
                var recordId = _recordIdSelector(record);
                if (!string.IsNullOrWhiteSpace(recordId))
                {
                    _recordsById[recordId] = record;
                }
            }
        }

        private void PruneTrackingForMissingRecords()
        {
            var knownIds = new HashSet<string>(_recordsById.Keys, StringComparer.Ordinal);
            PruneDictionary(_baselineValuesByRecord, knownIds);
            PruneDictionary(_changesByRecord, knownIds);
            PruneDictionary(_validationDetailsByRecord, knownIds);
            _editedRecordIds.RemoveWhere(recordId => !knownIds.Contains(recordId));
            _invalidRecordIds.RemoveWhere(recordId => !knownIds.Contains(recordId));

            if (CurrentSession != null)
            {
                foreach (var participant in CurrentSession.Participants.ToArray())
                {
                    if (!knownIds.Contains(participant.TargetId))
                    {
                        CurrentSession.RemoveParticipant(participant.TargetId);
                    }
                }

                if (!string.IsNullOrWhiteSpace(CurrentSession.EditingRecordId) && !knownIds.Contains(CurrentSession.EditingRecordId))
                {
                    CurrentSession.EditingRecordId = string.Empty;
                    CurrentSession.ActiveEditingFieldId = string.Empty;
                }
            }

            RecalculateValidationIssueCount();
        }

        private static void PruneDictionary<TValue>(IDictionary<string, TValue> source, ISet<string> knownIds)
        {
            foreach (var key in source.Keys.Where(key => !knownIds.Contains(key)).ToArray())
            {
                source.Remove(key);
            }
        }

        private object EnsureBaselineValue(string targetId, IEditSessionFieldDefinition fieldDefinition, TRecord record)
        {
            if (!_baselineValuesByRecord.TryGetValue(targetId, out var valuesByField))
            {
                valuesByField = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _baselineValuesByRecord[targetId] = valuesByField;
            }

            if (!valuesByField.TryGetValue(fieldDefinition.FieldId, out var originalValue))
            {
                originalValue = fieldDefinition.GetValue(record);
                valuesByField[fieldDefinition.FieldId] = originalValue;
            }

            return originalValue;
        }

        private void TrackFieldChange(string targetId, IEditSessionFieldDefinition fieldDefinition, object originalValue, object currentValue)
        {
            if (!_changesByRecord.TryGetValue(targetId, out var changesByField))
            {
                changesByField = new Dictionary<string, EditSessionFieldChange>(StringComparer.OrdinalIgnoreCase);
                _changesByRecord[targetId] = changesByField;
            }

            changesByField[fieldDefinition.FieldId] = new EditSessionFieldChange(fieldDefinition.FieldId, fieldDefinition.DisplayName, originalValue, currentValue);
            _editedRecordIds.Add(targetId);
        }

        private void ClearFieldChange(string targetId, string fieldId)
        {
            if (_baselineValuesByRecord.TryGetValue(targetId, out var baselineByField))
            {
                baselineByField.Remove(fieldId);
                if (baselineByField.Count == 0)
                {
                    _baselineValuesByRecord.Remove(targetId);
                }
            }

            if (_changesByRecord.TryGetValue(targetId, out var changesByField))
            {
                changesByField.Remove(fieldId);
                if (changesByField.Count == 0)
                {
                    _changesByRecord.Remove(targetId);
                    _editedRecordIds.Remove(targetId);
                }
            }
            else
            {
                _editedRecordIds.Remove(targetId);
            }
        }

        private ValidationSnapshot BuildValidationSnapshot(TRecord record, string editedFieldId = null, string editingText = null)
        {
            var cellErrors = new Dictionary<string, IReadOnlyCollection<GridValidationError>>(StringComparer.OrdinalIgnoreCase);
            foreach (var fieldDefinition in _fieldDefinitions)
            {
                if (fieldDefinition.ValidationConstraints == null)
                {
                    continue;
                }

                var result = _fieldValueValidator.Validate(new GridFieldValidationContext(
                    fieldDefinition.FieldId,
                    fieldDefinition.DisplayName,
                    fieldDefinition.ValueType,
                    fieldDefinition.ValidationConstraints,
                    fieldDefinition.GetValue(record),
                    string.Equals(fieldDefinition.FieldId, editedFieldId, StringComparison.OrdinalIgnoreCase) ? editingText : null,
                    fieldDefinition.EditorKind,
                    fieldDefinition.EditorItems,
                    fieldDefinition.EditorItemsMode));

                if (result.Errors.Count > 0)
                {
                    cellErrors[fieldDefinition.FieldId] = result.Errors
                        .Select(error => new GridValidationError(fieldDefinition.FieldId, error.Message, error.ErrorCode, error.MessageKey))
                        .ToArray();
                }
            }

            if (record is IDataErrorInfo dataErrorInfo)
            {
                if (!string.IsNullOrWhiteSpace(dataErrorInfo.Error))
                {
                    cellErrors[string.Empty] = new[] { new GridValidationError(string.Empty, dataErrorInfo.Error) };
                }

                foreach (var fieldDefinition in _fieldDefinitions)
                {
                    var error = dataErrorInfo[fieldDefinition.FieldId];
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        continue;
                    }

                    var existingErrors = cellErrors.TryGetValue(fieldDefinition.FieldId, out var item)
                        ? item
                        : Array.Empty<GridValidationError>();
                    cellErrors[fieldDefinition.FieldId] = existingErrors.Concat(new[] { new GridValidationError(fieldDefinition.FieldId, error) }).ToArray();
                }
            }

            return new ValidationSnapshot(cellErrors, BuildValidationDetails(cellErrors));
        }

        private void ApplyRecordValidationInternal(string targetId, TRecord record)
        {
            var snapshot = BuildValidationSnapshot(record);
            StoreValidationState(targetId, snapshot.CellErrorsByField, true, snapshot.Details);
        }

        private bool StoreValidationState(
            string targetId,
            IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField,
            bool wasValidated,
            IReadOnlyList<EditSessionValidationDetail> detailsOverride = null)
        {
            var details = detailsOverride ?? BuildValidationDetails(cellErrorsByField);
            var previousInvalid = _invalidRecordIds.Contains(targetId);
            var previousDetails = _validationDetailsByRecord.TryGetValue(targetId, out var existingDetailsBeforeUpdate)
                ? existingDetailsBeforeUpdate
                : Array.Empty<EditSessionValidationDetail>();
            var validationChanged = !ValidationDetailsEqual(previousDetails, details);

            if (_validationDetailsByRecord.TryGetValue(targetId, out var existingDetails))
            {
                _validationIssueCount -= existingDetails.Count;
            }

            if (details.Count == 0)
            {
                _validationDetailsByRecord.Remove(targetId);
            }
            else
            {
                _validationDetailsByRecord[targetId] = details;
                _validationIssueCount += details.Count;
            }

            var validationState = GridValidationStateMapper.ToRecordState(FlattenErrors(cellErrorsByField), wasValidated);
            if (validationState == RecordValidationState.Invalid)
            {
                _invalidRecordIds.Add(targetId);
            }
            else
            {
                _invalidRecordIds.Remove(targetId);
            }

            if (CurrentSession != null)
            {
                _stateMachine.ApplyValidationErrors(CurrentSession, targetId, cellErrorsByField, wasValidated);
            }

            return validationChanged || previousInvalid != _invalidRecordIds.Contains(targetId);
        }

        private IReadOnlyList<EditSessionValidationDetail> BuildValidationDetails(IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField)
        {
            if (cellErrorsByField == null || cellErrorsByField.Count == 0)
            {
                return Array.Empty<EditSessionValidationDetail>();
            }

            return cellErrorsByField
                .OrderBy(entry => ResolveFieldDisplayIndex(entry.Key))
                .SelectMany(entry => (entry.Value ?? Array.Empty<GridValidationError>())
                    .Where(error => error != null && !string.IsNullOrWhiteSpace(error.Message))
                    .Select(error => new EditSessionValidationDetail(entry.Key, ResolveFieldDisplayName(entry.Key), error.Message)))
                .ToArray();
        }

        private static IReadOnlyCollection<GridValidationError> FlattenErrors(IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField)
        {
            return cellErrorsByField == null
                ? Array.Empty<GridValidationError>()
                : cellErrorsByField.Values.Where(errors => errors != null).SelectMany(errors => errors).Where(error => error != null).ToArray();
        }

        private static bool ValidationDetailsEqual(
            IReadOnlyList<EditSessionValidationDetail> left,
            IReadOnlyList<EditSessionValidationDetail> right)
        {
            left = left ?? Array.Empty<EditSessionValidationDetail>();
            right = right ?? Array.Empty<EditSessionValidationDetail>();
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            for (var index = 0; index < left.Count; index++)
            {
                var leftItem = left[index];
                var rightItem = right[index];
                if (!string.Equals(leftItem?.FieldId, rightItem?.FieldId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(leftItem?.DisplayName, rightItem?.DisplayName, StringComparison.Ordinal) ||
                    !string.Equals(leftItem?.Message, rightItem?.Message, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private int ResolveFieldDisplayIndex(string fieldId)
        {
            for (var i = 0; i < _fieldDefinitions.Count; i++)
            {
                if (string.Equals(_fieldDefinitions[i].FieldId, fieldId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private string ResolveFieldDisplayName(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                return "Record";
            }

            return ResolveFieldDefinition(fieldId)?.DisplayName ?? fieldId;
        }

        private IEditSessionFieldDefinition ResolveFieldDefinition(string fieldId)
        {
            return string.IsNullOrWhiteSpace(fieldId)
                ? null
                : _fieldDefinitions.FirstOrDefault(field => string.Equals(field.FieldId, fieldId, StringComparison.OrdinalIgnoreCase));
        }

        private ChangeSet BuildChangeSet(EditSession session)
        {
            return new ChangeSet(
                Guid.NewGuid().ToString("N"),
                session.SessionId,
                _changesByRecord
                    .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => new ChangeSetChange(
                        ChangeTargetKind.Row,
                        entry.Key,
                        session.GetOrAddParticipant(entry.Key).TargetPath,
                        ChangeOperation.Update,
                        entry.Value.Values
                            .OrderBy(change => ResolveFieldDisplayIndex(change.FieldId))
                            .Select(change => new FieldChange(change.FieldId, change.OriginalValue, change.CurrentValue))
                            .ToArray(),
                        session.GetOrAddParticipant(entry.Key).VersionToken))
                    .ToArray());
        }

        private void ClearCommittedChangeTracking()
        {
            foreach (var recordId in _editedRecordIds.ToArray())
            {
                _baselineValuesByRecord.Remove(recordId);
                _changesByRecord.Remove(recordId);
                _invalidRecordIds.Remove(recordId);
                _validationDetailsByRecord.Remove(recordId);
            }

            _editedRecordIds.Clear();
            RecalculateValidationIssueCount();
        }

        private void ClearTrackedRecordState(string targetId)
        {
            _baselineValuesByRecord.Remove(targetId);
            _changesByRecord.Remove(targetId);
            _editedRecordIds.Remove(targetId);
            if (_validationDetailsByRecord.TryGetValue(targetId, out var details))
            {
                _validationIssueCount -= details.Count;
                _validationDetailsByRecord.Remove(targetId);
            }

            _invalidRecordIds.Remove(targetId);
        }

        private void RecalculateValidationIssueCount()
        {
            _validationIssueCount = _validationDetailsByRecord.Values.Sum(details => details?.Count ?? 0);
        }

        private void ApplyCommitStateToAllParticipants(RecordCommitState commitState, RecordCommitDetail commitDetail)
        {
            var session = RequireSession();
            foreach (var participant in session.Participants.ToArray())
            {
                _stateMachine.ApplyCommitState(session, participant.TargetId, commitState, commitDetail);
            }

            UpdateSurfaceProjection();
            RaiseStateChanged();
        }

        private void TryPruneParticipant(string targetId)
        {
            if (CurrentSession == null || !CurrentSession.TryGetParticipant(targetId, out var participant))
            {
                return;
            }

            if (string.Equals(CurrentSession.EditingRecordId, targetId, StringComparison.Ordinal))
            {
                return;
            }

            var hasDisplayState = participant.Cells.Values.Any(cell =>
                cell.DisplayState != CellDisplayState.Normal ||
                cell.AccessState != CellAccessState.Editable ||
                cell.ChangeState != CellChangeState.Unchanged);

            if (participant.EditState == RecordEditState.Unchanged &&
                participant.AccessState == RecordAccessState.Editable &&
                participant.CommitState == RecordCommitState.Idle &&
                !HasRecordChanges(targetId) &&
                !_validationDetailsByRecord.ContainsKey(targetId) &&
                !hasDisplayState)
            {
                CurrentSession.RemoveParticipant(targetId);
            }
        }

        private EditSession RequireSession()
        {
            ThrowIfDisposed();
            return CurrentSession ?? throw new InvalidOperationException("No active edit session.");
        }

        private EditSession EnsureSessionForMutation(string targetId)
        {
            ThrowIfDisposed();
            if (CurrentSession != null)
            {
                return CurrentSession;
            }

            var resolvedRootId = !string.IsNullOrWhiteSpace(targetId)
                ? targetId
                : (!string.IsNullOrWhiteSpace(CurrentRecordId) ? CurrentRecordId : "__edit-session__");
            CurrentSession = new EditSession(Guid.NewGuid().ToString("N"), EditSessionScopeKind.Aggregate, resolvedRootId, _dataSource.DefaultSaveMode);
            return CurrentSession;
        }

        private void UpdateSurfaceProjection()
        {
            SurfaceStateProjection = CurrentSession == null ? GridSurfaceStateProjection.Empty : GridSurfaceStateProjector.Project(CurrentSession);
        }

        private void ClearActiveFieldDisplayState(EditSession session)
        {
            if (session == null ||
                string.IsNullOrWhiteSpace(session.EditingRecordId) ||
                string.IsNullOrWhiteSpace(session.ActiveEditingFieldId) ||
                !session.TryGetParticipant(session.EditingRecordId, out _))
            {
                return;
            }

            _stateMachine.SetCellDisplayState(session, session.EditingRecordId, session.ActiveEditingFieldId, CellDisplayState.Normal);
        }

        private void ClearActiveFieldEditState(EditSession session, bool completeRecordEdit, bool clearParticipantDisplayOnly = false)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.EditingRecordId))
            {
                return;
            }

            var targetId = session.EditingRecordId;
            ClearActiveFieldDisplayState(session);
            session.ActiveEditingFieldId = string.Empty;
            session.EditingRecordId = string.Empty;

            if (clearParticipantDisplayOnly)
            {
                return;
            }

            if (completeRecordEdit && session.TryGetParticipant(targetId, out _))
            {
                _stateMachine.CompleteRecordEdit(session, targetId, HasRecordChanges(targetId));
                TryPruneParticipant(targetId);
            }
        }

        private void HandleDataSourceVersionChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        private void RaiseCurrentRecordChanged(string previousRecordId, TRecord previousRecord, string currentRecordId, TRecord currentRecord)
        {
            CurrentRecordChanged?.Invoke(this, new CurrentRecordChangedEventArgs<TRecord>(previousRecordId, previousRecord, currentRecordId, currentRecord));
        }

        private void RaiseStateChanged()
        {
            if (_stateChangeBatchDepth > 0)
            {
                _hasPendingStateChanged = true;
                return;
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CompleteStateChangeBatch()
        {
            if (_stateChangeBatchDepth <= 0)
            {
                throw new InvalidOperationException("Edit session state change batch was completed without an active batch.");
            }

            _stateChangeBatchDepth--;
            if (_stateChangeBatchDepth > 0 || !_hasPendingStateChanged)
            {
                return;
            }

            _hasPendingStateChanged = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EditSessionContext<TRecord>));
            }
        }

        IReadOnlyList<object> IEditSessionContext.Records => _recordObjects;

        object IEditSessionContext.CurrentRecord => CurrentRecord;

        private sealed class ValidationSnapshot
        {
            public ValidationSnapshot(IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> cellErrorsByField, IReadOnlyList<EditSessionValidationDetail> details)
            {
                CellErrorsByField = cellErrorsByField;
                Details = details;
            }

            public IReadOnlyDictionary<string, IReadOnlyCollection<GridValidationError>> CellErrorsByField { get; }

            public IReadOnlyList<EditSessionValidationDetail> Details { get; }
        }

        private sealed class StateChangeBatch : IDisposable
        {
            private EditSessionContext<TRecord> _owner;

            public StateChangeBatch(EditSessionContext<TRecord> owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                var owner = _owner;
                _owner = null;
                owner.CompleteStateChangeBatch();
            }
        }
    }
}
