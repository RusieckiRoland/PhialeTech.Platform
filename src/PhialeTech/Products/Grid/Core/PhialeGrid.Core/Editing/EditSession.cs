using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Commit;

namespace PhialeGrid.Core.Editing
{
    public sealed class EditSession
    {
        private readonly Dictionary<string, EditSessionParticipant> _participants =
            new Dictionary<string, EditSessionParticipant>(StringComparer.Ordinal);

        public EditSession(
            string sessionId,
            EditSessionScopeKind scopeKind,
            string rootTargetId,
            SaveMode saveMode)
        {
            SessionId = string.IsNullOrWhiteSpace(sessionId)
                ? throw new ArgumentException("Session id is required.", nameof(sessionId))
                : sessionId;
            ScopeKind = scopeKind;
            RootTargetId = string.IsNullOrWhiteSpace(rootTargetId)
                ? throw new ArgumentException("Root target id is required.", nameof(rootTargetId))
                : rootTargetId;
            SaveMode = saveMode;
            StartedAt = DateTimeOffset.UtcNow;
        }

        public string SessionId { get; }

        public EditSessionScopeKind ScopeKind { get; }

        public string RootTargetId { get; }

        public SaveMode SaveMode { get; }

        public DateTimeOffset StartedAt { get; }

        public string VersionToken { get; set; } = string.Empty;

        public string EditingRecordId { get; internal set; } = string.Empty;

        public string ActiveEditingFieldId { get; internal set; } = string.Empty;

        public IReadOnlyCollection<EditSessionParticipant> Participants => _participants.Values.ToArray();

        public bool IsDirty => _participants.Values.Any(participant => participant.IsDirty);

        public bool HasValidationErrors =>
            _participants.Values.Any(participant =>
                participant.ValidationState == RecordValidationState.Invalid ||
                participant.Cells.Values.Any(cell => cell.ValidationState == CellValidationState.Invalid));

        public EditSessionParticipant GetOrAddParticipant(string targetId, string targetPath = null)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("Target id is required.", nameof(targetId));
            }

            EditSessionParticipant participant;
            if (!_participants.TryGetValue(targetId, out participant))
            {
                participant = new EditSessionParticipant(targetId, targetPath);
                _participants[targetId] = participant;
            }

            return participant;
        }

        public bool TryGetParticipant(string targetId, out EditSessionParticipant participant)
        {
            return _participants.TryGetValue(targetId, out participant);
        }

        internal void RemoveParticipant(string targetId)
        {
            if (!string.IsNullOrWhiteSpace(targetId))
            {
                _participants.Remove(targetId);
            }
        }
    }
}
