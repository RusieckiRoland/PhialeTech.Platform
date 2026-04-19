using System;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Surface
{
    public sealed class GridRecordRenderState
    {
        public GridRecordRenderState(
            string recordKey,
            RecordEditState editState,
            RecordValidationState validationState,
            RecordAccessState accessState,
            RecordCommitState commitState,
            RecordCommitDetail commitDetail,
            string sessionId = null)
        {
            RecordKey = recordKey ?? throw new ArgumentNullException(nameof(recordKey));
            EditState = editState;
            ValidationState = validationState;
            AccessState = accessState;
            CommitState = commitState;
            CommitDetail = commitDetail;
            SessionId = sessionId ?? string.Empty;
        }

        public string RecordKey { get; }

        public RecordEditState EditState { get; }

        public RecordValidationState ValidationState { get; }

        public RecordAccessState AccessState { get; }

        public RecordCommitState CommitState { get; }

        public RecordCommitDetail CommitDetail { get; }

        public string SessionId { get; }
    }
}
