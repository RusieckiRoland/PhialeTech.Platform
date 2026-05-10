using NUnit.Framework;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Tests.Commit
{
    [TestFixture]
    public class ChangeSetCommitCoordinatorTests
    {
        [Test]
        public void Start_ForDirectMode_EntersPendingWithoutTccDetail()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Row, "row-1", SaveMode.Direct);
            var changeSet = new ChangeSet("changes-1", "session-1", new[] { CreateChange(ChangeTargetKind.Row, "row-1") });
            var sut = new ChangeSetCommitCoordinator();

            var state = sut.Start(session, changeSet);

            Assert.Multiple(() =>
            {
                Assert.That(state.SaveMode, Is.EqualTo(SaveMode.Direct));
                Assert.That(state.CommitState, Is.EqualTo(RecordCommitState.Pending));
                Assert.That(state.CommitDetail, Is.EqualTo(RecordCommitDetail.None));
            });
        }

        [Test]
        public void OptimisticConflict_MapsToRejectedVersionConflict()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Row, "row-1", SaveMode.Optimistic);
            var changeSet = new ChangeSet("changes-1", "session-1", new[] { CreateChange(ChangeTargetKind.Row, "row-1") });
            var sut = new ChangeSetCommitCoordinator();
            sut.Start(session, changeSet);

            var state = sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.OptimisticVersionConflict));

            Assert.Multiple(() =>
            {
                Assert.That(state.CommitState, Is.EqualTo(RecordCommitState.Rejected));
                Assert.That(state.CommitDetail, Is.EqualTo(RecordCommitDetail.VersionConflict));
                Assert.That(state.IsTerminal, Is.True);
            });
        }

        [Test]
        public void TccWorkflow_TransitionsThroughTryConfirmAndCommit()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Aggregate, "aggregate-1", SaveMode.Tcc);
            var changeSet = new ChangeSet("changes-1", "session-1", new[] { CreateChange(ChangeTargetKind.Aggregate, "aggregate-1") });
            var sut = new ChangeSetCommitCoordinator();

            var started = sut.Start(session, changeSet);
            var trySucceeded = sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.TccTrySucceededAwaitingConfirm));
            var confirmStarted = sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.TccConfirmStarted));
            var committed = sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.TccConfirmSucceeded));

            Assert.Multiple(() =>
            {
                Assert.That(started.CommitDetail, Is.EqualTo(RecordCommitDetail.TryPending));
                Assert.That(trySucceeded.CommitDetail, Is.EqualTo(RecordCommitDetail.TrySucceededAwaitingConfirm));
                Assert.That(confirmStarted.CommitDetail, Is.EqualTo(RecordCommitDetail.ConfirmPending));
                Assert.That(committed.CommitState, Is.EqualTo(RecordCommitState.Committed));
                Assert.That(committed.CommitDetail, Is.EqualTo(RecordCommitDetail.None));
            });
        }

        [Test]
        public void TccCancel_EndsAsRejectedCanceled()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Aggregate, "aggregate-1", SaveMode.Tcc);
            var changeSet = new ChangeSet("changes-1", "session-1", new[] { CreateChange(ChangeTargetKind.Aggregate, "aggregate-1") });
            var sut = new ChangeSetCommitCoordinator();
            sut.Start(session, changeSet);
            sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.TccTrySucceededAwaitingConfirm));
            sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.TccCancelStarted));

            var canceled = sut.ApplyOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.TccCancelSucceeded));

            Assert.Multiple(() =>
            {
                Assert.That(canceled.CommitState, Is.EqualTo(RecordCommitState.Rejected));
                Assert.That(canceled.CommitDetail, Is.EqualTo(RecordCommitDetail.Canceled));
            });
        }

        [Test]
        public void ChangeSet_CanRepresentRowAggregateAndDocumentChanges()
        {
            var changeSet = new ChangeSet(
                "changes-1",
                "session-1",
                new[]
                {
                    CreateChange(ChangeTargetKind.Row, "row-1"),
                    CreateChange(ChangeTargetKind.Aggregate, "aggregate-1"),
                    CreateChange(ChangeTargetKind.Document, "document-1"),
                });

            Assert.That(changeSet.Changes.Count, Is.EqualTo(3));
        }

        private static ChangeSetChange CreateChange(ChangeTargetKind kind, string targetId)
        {
            return new ChangeSetChange(
                kind,
                targetId,
                "/" + targetId,
                ChangeOperation.Update,
                new[]
                {
                    new FieldChange("Name", "Old", "New"),
                });
        }
    }
}

