using NUnit.Framework;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.Tests.Surface
{
    [TestFixture]
    public class GridSurfaceStateProjectorTests
    {
        [Test]
        public void Project_UsesCoreStatesWithoutInventingAdditionalSemantics()
        {
            var session = new EditSession("session-1", EditSessionScopeKind.Aggregate, "aggregate-1", SaveMode.Optimistic);
            var machine = new EditSessionStateMachine();

            machine.MarkRecordAsNew(session, "row-1");
            machine.ApplyRecordValidation(
                session,
                "row-1",
                RecordValidationState.Warning,
                new System.Collections.Generic.Dictionary<string, CellValidationState>
                {
                    ["Owner"] = CellValidationState.Invalid,
                });
            machine.SetCellDisplayState(session, "row-1", "Owner", CellDisplayState.Current);
            machine.MarkCellModified(session, "row-1", "Owner");
            machine.SetCellAccessState(session, "row-1", "Owner", CellAccessState.Locked);
            machine.ApplyCommitState(session, "row-1", RecordCommitState.Pending, RecordCommitDetail.TryPending);

            var projection = GridSurfaceStateProjector.Project(session);

            var record = projection.RecordStates["row-1"];
            var cell = projection.CellStates["row-1_Owner"];

            Assert.Multiple(() =>
            {
                Assert.That(record.EditState, Is.EqualTo(RecordEditState.New));
                Assert.That(record.ValidationState, Is.EqualTo(RecordValidationState.Warning));
                Assert.That(record.CommitState, Is.EqualTo(RecordCommitState.Pending));
                Assert.That(record.CommitDetail, Is.EqualTo(RecordCommitDetail.TryPending));
                Assert.That(cell.DisplayState, Is.EqualTo(CellDisplayState.Current));
                Assert.That(cell.ChangeState, Is.EqualTo(CellChangeState.Modified));
                Assert.That(cell.ValidationState, Is.EqualTo(CellValidationState.Invalid));
                Assert.That(cell.AccessState, Is.EqualTo(CellAccessState.Locked));
            });
        }
    }
}

