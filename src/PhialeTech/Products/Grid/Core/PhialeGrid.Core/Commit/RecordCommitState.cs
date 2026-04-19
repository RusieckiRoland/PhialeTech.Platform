namespace PhialeGrid.Core.Commit
{
    public enum RecordCommitState
    {
        Idle,
        Pending,
        Committed,
        Rejected,
        Failed,
    }
}
