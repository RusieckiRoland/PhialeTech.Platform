using System.Collections.Generic;
using NUnit.Framework;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;

namespace PhialeGrid.Core.Tests.Editing
{
    [TestFixture]
    public class EditSessionContextTests
    {
        [Test]
        public void SetCurrentRecord_ById_UpdatesCurrentRecordAndRaisesEvent()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
                new TestRecord("row-2", "Beta"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            CurrentRecordChangedEventArgs<TestRecord> args = null;
            sut.CurrentRecordChanged += (_, eventArgs) => args = eventArgs;

            var changed = sut.SetCurrentRecord("row-2");

            Assert.Multiple(() =>
            {
                Assert.That(changed, Is.True);
                Assert.That(sut.CurrentRecordId, Is.EqualTo("row-2"));
                Assert.That(sut.CurrentRecord.Name, Is.EqualTo("Beta"));
                Assert.That(args, Is.Not.Null);
                Assert.That(args.CurrentRecordId, Is.EqualTo("row-2"));
            });
        }

        [Test]
        public void StartSession_AndEditOperations_UpdateSurfaceProjection()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            sut.SetCurrentRecord("row-1");
            sut.StartSession(EditSessionScopeKind.Row, SaveMode.Direct);

            sut.BeginRecordEdit("row-1");
            sut.MarkCellModified("row-1", "Name");
            sut.SetCellDisplayState("row-1", "Name", CellDisplayState.Current);

            var recordState = sut.SurfaceStateProjection.RecordStates["row-1"];
            var cellState = sut.SurfaceStateProjection.CellStates["row-1_Name"];

            Assert.Multiple(() =>
            {
                Assert.That(recordState.EditState, Is.EqualTo(RecordEditState.Editing));
                Assert.That(cellState.ChangeState, Is.EqualTo(CellChangeState.Modified));
                Assert.That(cellState.DisplayState, Is.EqualTo(CellDisplayState.Current));
            });
        }

        [Test]
        public void ApplyValidationErrors_UpdatesSessionSummaryAndProjection()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            sut.SetCurrentRecord("row-1");
            sut.StartSession(EditSessionScopeKind.Row, SaveMode.Direct);
            sut.BeginRecordEdit("row-1");

            sut.ApplyValidationErrors(
                "row-1",
                new Dictionary<string, IReadOnlyCollection<GridValidationError>>
                {
                    ["Name"] = new[]
                    {
                        new GridValidationError("Name", "Name is required."),
                    },
                });

            var recordState = sut.SurfaceStateProjection.RecordStates["row-1"];
            var cellState = sut.SurfaceStateProjection.CellStates["row-1_Name"];

            Assert.Multiple(() =>
            {
                Assert.That(sut.CurrentSession.HasValidationErrors, Is.True);
                Assert.That(recordState.ValidationState, Is.EqualTo(RecordValidationState.Invalid));
                Assert.That(cellState.ValidationState, Is.EqualTo(CellValidationState.Invalid));
            });
        }

        [Test]
        public void BeginFieldEdit_PostActiveEdit_KeepsEditingDistinctFromEdited()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            sut.SetCurrentRecord("row-1");
            sut.StartSession(EditSessionScopeKind.Row, SaveMode.Direct);

            sut.BeginRecordEdit("row-1");
            sut.CompleteRecordEdit("row-1", hasEffectiveChange: true);
            var started = sut.BeginFieldEdit("row-1", "Name");
            sut.PostActiveEdit();

            var recordState = sut.SurfaceStateProjection.RecordStates["row-1"];

            Assert.Multiple(() =>
            {
                Assert.That(started, Is.True);
                Assert.That(sut.IsInEditMode, Is.False);
                Assert.That(recordState.EditState, Is.EqualTo(RecordEditState.Modified));
                Assert.That(sut.SurfaceStateProjection.EditingRecordId, Is.Empty);
                Assert.That(sut.SurfaceStateProjection.ActiveEditingFieldId, Is.Empty);
            });
        }

        [Test]
        public void BeginFieldEdit_CancelActiveEdit_ClearsEditingAndRestoresUnchangedStateWhenNoEffectiveChange()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            sut.SetCurrentRecord("row-1");
            sut.StartSession(EditSessionScopeKind.Row, SaveMode.Direct);

            sut.BeginFieldEdit("row-1", "Name");
            sut.CancelActiveEdit();

            Assert.Multiple(() =>
            {
                Assert.That(sut.IsInEditMode, Is.False);
                Assert.That(sut.SurfaceStateProjection.RecordStates.ContainsKey("row-1"), Is.False);
                Assert.That(sut.SurfaceStateProjection.EditingRecordId, Is.Empty);
                Assert.That(sut.SurfaceStateProjection.ActiveEditingFieldId, Is.Empty);
            });
        }

        [Test]
        public void CommitWorkflow_UpdatesParticipantsAndClearsModifiedStateAfterSuccess()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            sut.SetCurrentRecord("row-1");
            sut.StartSession(EditSessionScopeKind.Row, SaveMode.Optimistic);
            sut.BeginRecordEdit("row-1");
            sut.CompleteRecordEdit("row-1", hasEffectiveChange: true);
            sut.MarkCellModified("row-1", "Name");

            var changeSet = new ChangeSet(
                "changes-1",
                sut.CurrentSession.SessionId,
                new[]
                {
                    new ChangeSetChange(
                        ChangeTargetKind.Row,
                        "row-1",
                        "/row-1",
                        ChangeOperation.Update,
                        new[]
                        {
                            new FieldChange("Name", "Alpha", "Beta"),
                        }),
                });

            sut.StartCommit(changeSet);
            sut.ApplyCommitOutcome(new ChangeSetCommitOutcome(ChangeSetCommitOutcomeKind.OptimisticSucceeded));

            var recordState = sut.SurfaceStateProjection.RecordStates["row-1"];
            var cellState = sut.SurfaceStateProjection.CellStates["row-1_Name"];

            Assert.Multiple(() =>
            {
                Assert.That(sut.CommitState.CommitState, Is.EqualTo(RecordCommitState.Committed));
                Assert.That(recordState.EditState, Is.EqualTo(RecordEditState.Unchanged));
                Assert.That(recordState.CommitState, Is.EqualTo(RecordCommitState.Committed));
                Assert.That(cellState.ChangeState, Is.EqualTo(CellChangeState.Unchanged));
            });
        }

        [Test]
        public void DataSourceRefresh_PreservesCurrentRecordByIdWhenSnapshotChanges()
        {
            var source = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("row-1", "Alpha"),
            });
            var sut = new EditSessionContext<TestRecord>(source, record => record.Id);
            sut.SetCurrentRecord("row-1");

            source.ReplaceSnapshot(new[]
            {
                new TestRecord("row-1", "Alpha v2"),
            });

            Assert.Multiple(() =>
            {
                Assert.That(sut.CurrentRecordId, Is.EqualTo("row-1"));
                Assert.That(sut.CurrentRecord.Name, Is.EqualTo("Alpha v2"));
            });
        }

        [Test]
        public void DetailContext_CanListenToParentCurrentRecordChangedWithoutSharingSession()
        {
            var masterSource = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("parent-1", "Parent 1"),
                new TestRecord("parent-2", "Parent 2"),
            });
            var detailSource = new InMemoryEditSessionDataSource<TestRecord>(new[]
            {
                new TestRecord("detail-a", "Detail A"),
            });
            var masterContext = new EditSessionContext<TestRecord>(masterSource, record => record.Id);
            var detailContext = new EditSessionContext<TestRecord>(detailSource, record => record.Id);
            string receivedParentRecordId = null;

            masterContext.CurrentRecordChanged += (_, args) =>
            {
                receivedParentRecordId = args.CurrentRecordId;
                detailSource.ReplaceSnapshot(new[]
                {
                    new TestRecord("detail-for-" + args.CurrentRecordId, "Detail for " + args.CurrentRecordId),
                });
            };

            masterContext.SetCurrentRecord("parent-2");

            Assert.Multiple(() =>
            {
                Assert.That(receivedParentRecordId, Is.EqualTo("parent-2"));
                Assert.That(detailContext.Records.Count, Is.EqualTo(1));
                Assert.That(detailContext.Records[0].Id, Is.EqualTo("detail-for-parent-2"));
                Assert.That(masterContext.CurrentSession, Is.Null);
                Assert.That(detailContext.CurrentSession, Is.Null);
            });
        }

        [Test]
        public void BeginStateChangeBatch_WhenMultipleFieldValuesChange_RaisesSingleStateChanged()
        {
            var records = new[]
            {
                new MutableTestRecord("row-1", "Alpha", "Owner A"),
            };
            var fields = new IEditSessionFieldDefinition[]
            {
                new EditSessionFieldDefinition<MutableTestRecord>(
                    "Name",
                    "Name",
                    typeof(string),
                    record => record.Name,
                    (record, value) => record.Name = value as string),
                new EditSessionFieldDefinition<MutableTestRecord>(
                    "Owner",
                    "Owner",
                    typeof(string),
                    record => record.Owner,
                    (record, value) => record.Owner = value as string),
            };
            var source = new InMemoryEditSessionDataSource<MutableTestRecord>(records, fields);
            var sut = new EditSessionContext<MutableTestRecord>(source, record => record.Id);
            var stateChangedCount = 0;
            sut.StateChanged += (sender, args) => stateChangedCount++;

            using (sut.BeginStateChangeBatch("multi-field-edit"))
            {
                Assert.That(sut.TrySetFieldValue("row-1", "Name", "Beta"), Is.True);
                Assert.That(sut.TrySetFieldValue("row-1", "Owner", "Owner B"), Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(stateChangedCount, Is.EqualTo(1));
                Assert.That(sut.SurfaceStateProjection.RecordStates["row-1"].EditState, Is.EqualTo(RecordEditState.Modified));
                Assert.That(sut.SurfaceStateProjection.CellStates["row-1_Name"].ChangeState, Is.EqualTo(CellChangeState.Modified));
                Assert.That(sut.SurfaceStateProjection.CellStates["row-1_Owner"].ChangeState, Is.EqualTo(CellChangeState.Modified));
            });
        }

        private sealed class TestRecord
        {
            public TestRecord(string id, string name)
            {
                Id = id;
                Name = name;
            }

            public string Id { get; }

            public string Name { get; }
        }

        private sealed class MutableTestRecord
        {
            public MutableTestRecord(string id, string name, string owner)
            {
                Id = id;
                Name = name;
                Owner = owner;
            }

            public string Id { get; }

            public string Name { get; set; }

            public string Owner { get; set; }
        }
    }
}
