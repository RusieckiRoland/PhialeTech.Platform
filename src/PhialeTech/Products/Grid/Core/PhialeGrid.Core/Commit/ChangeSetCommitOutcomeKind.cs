namespace PhialeGrid.Core.Commit
{
    public enum ChangeSetCommitOutcomeKind
    {
        DirectSucceeded,
        DirectValidationRejected,
        DirectBusinessRejected,
        DirectTechnicalFailed,
        OptimisticSucceeded,
        OptimisticVersionConflict,
        OptimisticValidationRejected,
        OptimisticBusinessRejected,
        OptimisticTechnicalFailed,
        TccTrySucceededAwaitingConfirm,
        TccConfirmStarted,
        TccConfirmSucceeded,
        TccCancelStarted,
        TccCancelSucceeded,
        TccTechnicalFailed,
    }
}
