using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Editing;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridEditSessionTests
    {
        [Test]
        public async Task CommitAsync_ReturnsModifiedRowsAndValidationErrors()
        {
            var row = new TestRow { Id = "1", Name = "Alice", Age = 30, Amount = 100m };
            var session = new GridEditSession<TestRow>(CreateEditor(), x => x.Id, new TestValidator());

            session.BeginEdit(row);
            session.SetCellValue(row, "Name", "");
            session.SetCellValue(row, "Amount", 120m);

            var result = await session.CommitAsync();
            var change = result.Single();

            Assert.That(change.ChangeType, Is.EqualTo(GridRowChangeType.Modified));
            Assert.That(change.IsValid, Is.False);
            Assert.That(change.Errors.Count, Is.EqualTo(1));
            Assert.That(session.DirtyRowIds.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CommitAsync_ClearsDirtyFlagAfterValidCommit()
        {
            var row = new TestRow { Id = "1", Name = "Alice", Age = 30, Amount = 100m };
            var session = new GridEditSession<TestRow>(CreateEditor(), x => x.Id, new TestValidator());

            session.BeginEdit(row);
            session.SetCellValue(row, "Name", "Alice 2");

            var result = await session.CommitAsync();
            Assert.That(result.Single().IsValid, Is.True);
            Assert.That(session.DirtyRowIds.Count, Is.EqualTo(0));
        }

        [Test]
        public void EditHistory_TracksUndoRedo()
        {
            var value = 0;
            var history = new GridEditHistory();
            history.Execute(new DelegateCommand(() => value = 5, () => value = 0));
            history.Execute(new DelegateCommand(() => value = 7, () => value = 5));

            Assert.That(value, Is.EqualTo(7));
            Assert.That(history.Undo(), Is.True);
            Assert.That(value, Is.EqualTo(5));
            Assert.That(history.Undo(), Is.True);
            Assert.That(value, Is.EqualTo(0));
            Assert.That(history.Redo(), Is.True);
            Assert.That(value, Is.EqualTo(5));
        }

        private static IGridRowEditor<TestRow> CreateEditor()
        {
            return new DelegateGridRowEditor<TestRow>(
                (row, col) =>
                {
                    switch (col)
                    {
                        case "Name": return row.Name;
                        case "Amount": return row.Amount;
                        default: return null;
                    }
                },
                (row, col, val) =>
                {
                    switch (col)
                    {
                        case "Name": row.Name = Convert.ToString(val); break;
                        case "Amount": row.Amount = Convert.ToDecimal(val); break;
                    }
                },
                row => new TestRow
                {
                    Id = row.Id,
                    Name = row.Name,
                    Age = row.Age,
                    City = row.City,
                    Active = row.Active,
                    Amount = row.Amount,
                });
        }

        private sealed class TestValidator : IGridRowValidator<TestRow>
        {
            public Task<IReadOnlyList<GridValidationError>> ValidateAsync(TestRow row, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    return Task.FromResult<IReadOnlyList<GridValidationError>>(new[] { new GridValidationError("Name", "Required") });
                }

                return Task.FromResult<IReadOnlyList<GridValidationError>>(Array.Empty<GridValidationError>());
            }
        }

        private sealed class DelegateCommand : IGridEditCommand
        {
            private readonly Action _execute;
            private readonly Action _undo;

            public DelegateCommand(Action execute, Action undo)
            {
                _execute = execute;
                _undo = undo;
            }

            public void Execute() => _execute();

            public void Undo() => _undo();
        }
    }
}

