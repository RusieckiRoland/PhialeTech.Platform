using System;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Commit
{
    public sealed class ChangeSetCommitCoordinator : IChangeSetCommitCoordinator
    {
        public ChangeSetCommitState CurrentState { get; private set; }

        public ChangeSetCommitState Start(EditSession session, ChangeSet changeSet)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }

            if (!string.Equals(session.SessionId, changeSet.SessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Change set session id must match edit session id.");
            }

            var detail = session.SaveMode == SaveMode.Tcc
                ? RecordCommitDetail.TryPending
                : RecordCommitDetail.None;

            CurrentState = new ChangeSetCommitState(
                session.SessionId,
                changeSet.ChangeSetId,
                session.SaveMode,
                RecordCommitState.Pending,
                detail,
                DateTimeOffset.UtcNow);

            return CurrentState;
        }

        public ChangeSetCommitState ApplyOutcome(ChangeSetCommitOutcome outcome)
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException("Commit workflow has not been started.");
            }

            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            var nextState = ResolveState(CurrentState, outcome.Kind);
            CurrentState = new ChangeSetCommitState(
                CurrentState.SessionId,
                CurrentState.ChangeSetId,
                CurrentState.SaveMode,
                nextState.commitState,
                nextState.commitDetail,
                DateTimeOffset.UtcNow);

            return CurrentState;
        }

        private static (RecordCommitState commitState, RecordCommitDetail commitDetail) ResolveState(
            ChangeSetCommitState currentState,
            ChangeSetCommitOutcomeKind outcomeKind)
        {
            switch (outcomeKind)
            {
                case ChangeSetCommitOutcomeKind.DirectSucceeded:
                case ChangeSetCommitOutcomeKind.OptimisticSucceeded:
                case ChangeSetCommitOutcomeKind.TccConfirmSucceeded:
                    return (RecordCommitState.Committed, RecordCommitDetail.None);

                case ChangeSetCommitOutcomeKind.DirectValidationRejected:
                case ChangeSetCommitOutcomeKind.OptimisticValidationRejected:
                    return (RecordCommitState.Rejected, RecordCommitDetail.ValidationRejected);

                case ChangeSetCommitOutcomeKind.DirectBusinessRejected:
                case ChangeSetCommitOutcomeKind.OptimisticBusinessRejected:
                    return (RecordCommitState.Rejected, RecordCommitDetail.BusinessRuleRejected);

                case ChangeSetCommitOutcomeKind.DirectTechnicalFailed:
                case ChangeSetCommitOutcomeKind.OptimisticTechnicalFailed:
                case ChangeSetCommitOutcomeKind.TccTechnicalFailed:
                    return (RecordCommitState.Failed, RecordCommitDetail.TechnicalError);

                case ChangeSetCommitOutcomeKind.OptimisticVersionConflict:
                    return (RecordCommitState.Rejected, RecordCommitDetail.VersionConflict);

                case ChangeSetCommitOutcomeKind.TccTrySucceededAwaitingConfirm:
                    EnsureSaveMode(currentState, SaveMode.Tcc, outcomeKind);
                    return (RecordCommitState.Pending, RecordCommitDetail.TrySucceededAwaitingConfirm);

                case ChangeSetCommitOutcomeKind.TccConfirmStarted:
                    EnsureSaveMode(currentState, SaveMode.Tcc, outcomeKind);
                    return (RecordCommitState.Pending, RecordCommitDetail.ConfirmPending);

                case ChangeSetCommitOutcomeKind.TccCancelStarted:
                    EnsureSaveMode(currentState, SaveMode.Tcc, outcomeKind);
                    return (RecordCommitState.Pending, RecordCommitDetail.CancelPending);

                case ChangeSetCommitOutcomeKind.TccCancelSucceeded:
                    EnsureSaveMode(currentState, SaveMode.Tcc, outcomeKind);
                    return (RecordCommitState.Rejected, RecordCommitDetail.Canceled);

                default:
                    throw new InvalidOperationException("Unsupported commit outcome: " + outcomeKind);
            }
        }

        private static void EnsureSaveMode(
            ChangeSetCommitState currentState,
            SaveMode expectedSaveMode,
            ChangeSetCommitOutcomeKind outcomeKind)
        {
            if (currentState.SaveMode != expectedSaveMode)
            {
                throw new InvalidOperationException(
                    "Commit outcome " + outcomeKind + " is valid only for " + expectedSaveMode + " workflows.");
            }
        }
    }
}
