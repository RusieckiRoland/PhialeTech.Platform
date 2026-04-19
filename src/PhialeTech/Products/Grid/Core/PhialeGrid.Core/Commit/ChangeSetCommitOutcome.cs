using System;

namespace PhialeGrid.Core.Commit
{
    public sealed class ChangeSetCommitOutcome
    {
        public ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind kind, string message = null)
        {
            Kind = kind;
            Message = message ?? string.Empty;
        }

        public ChangeSetCommitOutcomeKind Kind { get; }

        public string Message { get; }
    }
}
