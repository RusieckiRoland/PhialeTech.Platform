using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Commit
{
    public interface IChangeSetCommitCoordinator
    {
        ChangeSetCommitState CurrentState { get; }

        ChangeSetCommitState Start(EditSession session, ChangeSet changeSet);

        ChangeSetCommitState ApplyOutcome(ChangeSetCommitOutcome outcome);
    }
}
