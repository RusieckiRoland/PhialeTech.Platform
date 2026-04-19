namespace PhialeGrid.Core.Commit
{
    public enum RecordCommitDetail
    {
        None,
        VersionConflict,
        ValidationRejected,
        BusinessRuleRejected,
        TryPending,
        TrySucceededAwaitingConfirm,
        ConfirmPending,
        CancelPending,
        Canceled,
        TechnicalError,
    }
}
