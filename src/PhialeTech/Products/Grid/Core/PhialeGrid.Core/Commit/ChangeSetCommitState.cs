using System;

namespace PhialeGrid.Core.Commit
{
    public sealed class ChangeSetCommitState
    {
        public ChangeSetCommitState(
            string sessionId,
            string changeSetId,
            SaveMode saveMode,
            RecordCommitState commitState,
            RecordCommitDetail commitDetail,
            DateTimeOffset updatedAt)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            ChangeSetId = changeSetId ?? throw new ArgumentNullException(nameof(changeSetId));
            SaveMode = saveMode;
            CommitState = commitState;
            CommitDetail = commitDetail;
            UpdatedAt = updatedAt;
        }

        public string SessionId { get; }

        public string ChangeSetId { get; }

        public SaveMode SaveMode { get; }

        public RecordCommitState CommitState { get; }

        public RecordCommitDetail CommitDetail { get; }

        public DateTimeOffset UpdatedAt { get; }

        public bool IsTerminal =>
            CommitState == RecordCommitState.Committed ||
            CommitState == RecordCommitState.Rejected ||
            CommitState == RecordCommitState.Failed;
    }
}
