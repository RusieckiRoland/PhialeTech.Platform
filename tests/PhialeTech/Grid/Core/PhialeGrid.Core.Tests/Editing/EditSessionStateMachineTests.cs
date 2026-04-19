using NUnit.Framework;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Tests.Editing
{
    [TestFixture]
    public class EditSessionStateMachineTests
    {
        [Test]
        public void BeginAndCancelEdit_WithoutEffectiveChange_ReturnsParticipantToUnchanged()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Row, "row-1", SaveMode.Direct);
            var sut = new EditSessionStateMachine();
            session.GetOrAddParticipant("row-1");

            sut.BeginRecordEdit(session, "row-1");
            var participant = sut.CancelRecordEdit(session, "row-1");

            Assert.Multiple(() =>
            {
                Assert.That(participant.EditState, Is.EqualTo(RecordEditState.Unchanged));
                Assert.That(session.IsDirty, Is.False);
            });
        }

        [Test]
        public void BeginAndCompleteEdit_WithEffectiveChange_MarksParticipantAsModified()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Row, "row-1", SaveMode.Direct);
            var sut = new EditSessionStateMachine();
            session.GetOrAddParticipant("row-1");

            sut.BeginRecordEdit(session, "row-1");
            var participant = sut.CompleteRecordEdit(session, "row-1", hasEffectiveChange: true);

            Assert.Multiple(() =>
            {
                Assert.That(participant.EditState, Is.EqualTo(RecordEditState.Modified));
                Assert.That(session.IsDirty, Is.True);
            });
        }

        [Test]
        public void NewParticipant_RemainsNewAfterEditingRoundTrip()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Aggregate, "aggregate-1", SaveMode.Optimistic);
            var sut = new EditSessionStateMachine();

            sut.MarkRecordAsNew(session, "detail-1");
            sut.BeginRecordEdit(session, "detail-1");
            var participant = sut.CompleteRecordEdit(session, "detail-1", hasEffectiveChange: true);

            Assert.That(participant.EditState, Is.EqualTo(RecordEditState.New));
        }

        [Test]
        public void MarkedForDelete_RemainsDistinctFromModified()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Aggregate, "aggregate-1", SaveMode.Optimistic);
            var sut = new EditSessionStateMachine();

            var participant = sut.MarkRecordForDelete(session, "detail-1");

            Assert.Multiple(() =>
            {
                Assert.That(participant.EditState, Is.EqualTo(RecordEditState.MarkedForDelete));
                Assert.That(session.IsDirty, Is.True);
            });
        }

        [Test]
        public void CellStateChanges_AreTrackedIndependentlyFromRecordDisplayState()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Row, "row-1", SaveMode.Direct);
            var sut = new EditSessionStateMachine();
            session.GetOrAddParticipant("row-1");

            sut.SetCellDisplayState(session, "row-1", "Owner", CellDisplayState.Current);
            sut.MarkCellModified(session, "row-1", "Owner");

            var participant = session.GetOrAddParticipant("row-1");
            var cell = participant.Cells["Owner"];

            Assert.Multiple(() =>
            {
                Assert.That(cell.DisplayState, Is.EqualTo(CellDisplayState.Current));
                Assert.That(cell.ChangeState, Is.EqualTo(CellChangeState.Modified));
                Assert.That(session.IsDirty, Is.True);
            });
        }

        [Test]
        public void ApplyValidationErrors_UpdatesRecordAndCellValidationSummary()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Row, "row-1", SaveMode.Direct);
            var sut = new EditSessionStateMachine();
            session.GetOrAddParticipant("row-1");

            sut.ApplyValidationErrors(
                session,
                "row-1",
                new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyCollection<GridValidationError>>
                {
                    ["Owner"] = new[]
                    {
                        new GridValidationError("Owner", "Owner is required."),
                    },
                    ["Priority"] = new[]
                    {
                        new GridValidationError("Priority", "Suspicious priority.", severity: GridValidationSeverity.Warning),
                    },
                });

            var participant = session.GetOrAddParticipant("row-1");

            Assert.Multiple(() =>
            {
                Assert.That(participant.ValidationState, Is.EqualTo(RecordValidationState.Invalid));
                Assert.That(participant.Cells["Owner"].ValidationState, Is.EqualTo(CellValidationState.Invalid));
                Assert.That(participant.Cells["Priority"].ValidationState, Is.EqualTo(CellValidationState.Warning));
                Assert.That(session.HasValidationErrors, Is.True);
            });
        }

        [Test]
        public void ApplySuccessfulCommit_ClearsModifiedParticipantsAndRemovesDeletedOnes()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Aggregate, "aggregate-1", SaveMode.Optimistic);
            var sut = new EditSessionStateMachine();
            session.GetOrAddParticipant("row-1");
            sut.BeginRecordEdit(session, "row-1");
            sut.CompleteRecordEdit(session, "row-1", hasEffectiveChange: true);
            sut.MarkCellModified(session, "row-1", "Owner");
            sut.MarkRecordForDelete(session, "detail-1");

            sut.ApplySuccessfulCommit(session);

            EditSessionParticipant remainingParticipant;
            var hasDeletedParticipant = session.TryGetParticipant("detail-1", out remainingParticipant);
            var rowParticipant = session.GetOrAddParticipant("row-1");

            Assert.Multiple(() =>
            {
                Assert.That(rowParticipant.EditState, Is.EqualTo(RecordEditState.Unchanged));
                Assert.That(rowParticipant.CommitState, Is.EqualTo(RecordCommitState.Committed));
                Assert.That(rowParticipant.Cells["Owner"].ChangeState, Is.EqualTo(CellChangeState.Unchanged));
                Assert.That(hasDeletedParticipant, Is.False);
                Assert.That(session.IsDirty, Is.False);
            });
        }
    }
}
