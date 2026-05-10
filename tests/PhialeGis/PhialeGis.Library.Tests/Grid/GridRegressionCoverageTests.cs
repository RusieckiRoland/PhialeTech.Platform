using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Clipboard;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Export;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Core.Virtualization;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridRegressionCoverageTests
    {
        [Test]
        public async Task Regression01_SharedFetchCancellation_ShouldNotCancelOtherWaiters()
        {
            var provider = new SharedCancellationProvider();
            var source = new VirtualizedGridDataSource<int>(provider, pageSize: 10, maxCachedPages: 4, prefetchPageRadius: 0);
            var firstCts = new CancellationTokenSource();
            var secondCts = new CancellationTokenSource();

            var firstTask = source.GetItemAsync(5, firstCts.Token);
            await provider.Started.Task.ConfigureAwait(false);
            var secondTask = source.GetItemAsync(6, secondCts.Token);

            firstCts.Cancel();
            provider.Release.TrySetResult(true);

            Assert.That(async () => await firstTask.ConfigureAwait(false), Throws.InstanceOf<OperationCanceledException>());
            Assert.That(async () => await secondTask.ConfigureAwait(false), Throws.Nothing);
            Assert.That(await secondTask.ConfigureAwait(false), Is.EqualTo(6));
        }

        [Test]
        public void Regression02_Viewport_ShouldReturnNoColumnsWhenScrolledPastRightEdge()
        {
            var viewport = new GridViewport(
                horizontalOffset: 1000,
                verticalOffset: 0,
                viewportWidth: 250,
                viewportHeight: 100,
                rowHeight: 20,
                columnWidths: new[] { 100d, 100d, 100d });

            var range = viewport.CalculateVisibleColumns(overscan: 0);

            Assert.That(range.Start, Is.EqualTo(3));
            Assert.That(range.Length, Is.EqualTo(0));
        }

        [Test]
        public void Regression03_QueryEngine_ShouldTreatMixedNumericTypesNumerically()
        {
            var rows = new[]
            {
                new ObjectValueRow("a", 100m),
                new ObjectValueRow("b", 20),
                new ObjectValueRow("c", 3L),
            };
            var engine = CreateObjectValueEngine();

            var sorted = engine.Execute(
                rows,
                new GridQueryRequest(
                    0,
                    10,
                    new[] { new GridSortDescriptor("Value", GridSortDirection.Ascending) },
                    GridFilterGroup.EmptyAnd(),
                    Array.Empty<GridGroupDescriptor>(),
                    Array.Empty<GridSummaryDescriptor>()));

            var filtered = engine.Execute(
                rows,
                new GridQueryRequest(
                    0,
                    10,
                    new[] { new GridSortDescriptor("Value", GridSortDirection.Ascending) },
                    new GridFilterGroup(new[] { new GridFilterDescriptor("Value", GridFilterOperator.GreaterThan, 10m) }, GridLogicalOperator.And),
                    Array.Empty<GridGroupDescriptor>(),
                    Array.Empty<GridSummaryDescriptor>()));

            Assert.That(sorted.Items.Select(x => Convert.ToDecimal(x.Value, CultureInfo.InvariantCulture)).ToArray(), Is.EqualTo(new[] { 3m, 20m, 100m }));
            Assert.That(filtered.Items.Select(x => Convert.ToDecimal(x.Value, CultureInfo.InvariantCulture)).ToArray(), Is.EqualTo(new[] { 20m, 100m }));
        }

        [Test]
        public void Regression04_IsTrueFilter_ShouldIgnoreInvalidBooleanRepresentationsInsteadOfThrowing()
        {
            var rows = new[]
            {
                new ObjectValueRow("a", string.Empty),
                new ObjectValueRow("b", "Y"),
                new ObjectValueRow("c", true),
            };
            var engine = CreateObjectValueEngine();
            GridQueryResult<ObjectValueRow> result = null;

            Assert.That(
                () => result = engine.Execute(
                    rows,
                    new GridQueryRequest(
                        0,
                        10,
                        Array.Empty<GridSortDescriptor>(),
                        new GridFilterGroup(new[] { new GridFilterDescriptor("Value", GridFilterOperator.IsTrue) }, GridLogicalOperator.And),
                        Array.Empty<GridGroupDescriptor>(),
                        Array.Empty<GridSummaryDescriptor>())),
                Throws.Nothing);

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items.Single().Id, Is.EqualTo("c"));
        }

        [Test]
        public void Regression05_StateCodec_ShouldRoundTripComplexFiltersWithoutDataLoss()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Name", "Name", displayIndex: 0),
                new GridColumnDefinition("Age", "Age", displayIndex: 1),
            };
            var snapshot = new GridStateSnapshot(
                new GridLayoutSnapshot(columns),
                new[] { new GridSortDescriptor("Name", GridSortDirection.Descending) },
                new GridFilterGroup(
                    new[]
                    {
                        new GridFilterDescriptor("Name", GridFilterOperator.Contains, "A,B|C;D"),
                        new GridFilterDescriptor("Age", GridFilterOperator.Between, 18, 65),
                    },
                    GridLogicalOperator.Or),
                new[] { new GridGroupDescriptor("Name") },
                new[] { new GridSummaryDescriptor("Age", GridSummaryType.Max) });

            var encoded = GridStateCodec.Encode(snapshot);
            var decoded = GridStateCodec.Decode(encoded, columns);

            Assert.That(decoded.Filters.LogicalOperator, Is.EqualTo(GridLogicalOperator.Or));
            Assert.That(decoded.Filters.Filters.Count, Is.EqualTo(2));
            Assert.That(decoded.Filters.Filters[0].Value, Is.EqualTo("A,B|C;D"));
            Assert.That(Convert.ToInt32(decoded.Filters.Filters[1].SecondValue, CultureInfo.InvariantCulture), Is.EqualTo(65));
        }

        [Test]
        public void Regression06_CsvImporter_ShouldHandleQuotedMultilineCells()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "line1\nline2", City = "Warsaw" },
            };

            var csv = GridCsvExporter.Export(
                rows,
                new[] { "Id", "Name", "City" },
                (row, columnId) =>
                {
                    switch (columnId)
                    {
                        case "Id": return row.Id;
                        case "Name": return row.Name;
                        case "City": return row.City;
                        default: return null;
                    }
                });

            var imported = GridCsvImporter.Import(csv);

            Assert.That(imported.Count, Is.EqualTo(1));
            Assert.That(imported[0]["Name"], Is.EqualTo("line1\nline2"));
        }

        [Test]
        public void Regression07_ClipboardCodec_ShouldRoundTripTabsAndNewLinesLosslessly()
        {
            var original = new[]
            {
                (IReadOnlyList<string>)new[] { "a\tb", "line1\nline2" },
            };

            var encoded = GridClipboardCodec.Encode(original);
            var decoded = GridClipboardCodec.Decode(encoded);

            Assert.That(decoded.Count, Is.EqualTo(1));
            Assert.That(decoded[0][0], Is.EqualTo("a\tb"));
            Assert.That(decoded[0][1], Is.EqualTo("line1\nline2"));
        }

        [Test]
        public async Task Regression08_CancelChanges_ForNewRow_ShouldDiscardDraftCompletely()
        {
            var row = new TestRow { Id = "new-1", Name = "Draft", Age = 1 };
            var session = new GridEditSession<TestRow>(CreateRowEditor(), x => x.Id);

            session.MarkAsNew(row);
            session.CancelChanges(row.Id);
            var committed = await session.CommitAsync().ConfigureAwait(false);

            Assert.That(session.DirtyRowIds, Is.Empty);
            Assert.That(committed, Is.Empty);
            Assert.That(() => session.GetWorkingRow(row.Id), Throws.InvalidOperationException);
        }

        [Test]
        public async Task Regression09_CommitDeletedRow_ShouldStopTrackingItAfterSuccessfulCommit()
        {
            var row = new TestRow { Id = "row-1", Name = "Alice", Age = 30 };
            var session = new GridEditSession<TestRow>(CreateRowEditor(), x => x.Id);

            session.MarkAsDeleted(row);
            var firstCommit = await session.CommitAsync().ConfigureAwait(false);
            var secondCommit = await session.CommitAsync().ConfigureAwait(false);

            Assert.That(firstCommit.Count, Is.EqualTo(1));
            Assert.That(firstCommit[0].ChangeType, Is.EqualTo(GridRowChangeType.Deleted));
            Assert.That(secondCommit, Is.Empty);
            Assert.That(() => session.GetWorkingRow(row.Id), Throws.InvalidOperationException);
        }

        [Test]
        public void Regression10_SummaryMinMax_ShouldHandleMixedNumericTypes()
        {
            var rows = new[]
            {
                new ObjectValueRow("a", 100m),
                new ObjectValueRow("b", 20),
                new ObjectValueRow("c", 3L),
            };
            var descriptors = new[]
            {
                new GridSummaryDescriptor("Value", GridSummaryType.Min),
                new GridSummaryDescriptor("Value", GridSummaryType.Max),
            };
            GridSummarySet result = null;

            Assert.That(() => result = GridSummaryEngine.Calculate(rows, descriptors, new DelegateGridRowAccessor<ObjectValueRow>((row, _) => row.Value)), Throws.Nothing);
            Assert.That(Convert.ToDecimal(result["Value:Min"], CultureInfo.InvariantCulture), Is.EqualTo(3m));
            Assert.That(Convert.ToDecimal(result["Value:Max"], CultureInfo.InvariantCulture), Is.EqualTo(100m));
        }

        private static GridQueryEngine<ObjectValueRow> CreateObjectValueEngine()
        {
            return new GridQueryEngine<ObjectValueRow>(new DelegateGridRowAccessor<ObjectValueRow>((row, _) => row.Value));
        }

        private static IGridRowEditor<TestRow> CreateRowEditor()
        {
            return new DelegateGridRowEditor<TestRow>(
                (row, columnId) =>
                {
                    switch (columnId)
                    {
                        case "Id": return row.Id;
                        case "Name": return row.Name;
                        case "Age": return row.Age;
                        case "City": return row.City;
                        case "Active": return row.Active;
                        case "Amount": return row.Amount;
                        default: return null;
                    }
                },
                (row, columnId, value) =>
                {
                    switch (columnId)
                    {
                        case "Id":
                            row.Id = Convert.ToString(value, CultureInfo.InvariantCulture);
                            break;
                        case "Name":
                            row.Name = Convert.ToString(value, CultureInfo.InvariantCulture);
                            break;
                        case "Age":
                            row.Age = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                            break;
                        case "City":
                            row.City = Convert.ToString(value, CultureInfo.InvariantCulture);
                            break;
                        case "Active":
                            row.Active = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                            break;
                        case "Amount":
                            row.Amount = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                            break;
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

        private sealed class ObjectValueRow
        {
            public ObjectValueRow(string id, object value)
            {
                Id = id;
                Value = value;
            }

            public string Id { get; }

            public object Value { get; }
        }

        private sealed class SharedCancellationProvider : IGridDataPageProvider<int>
        {
            public SharedCancellationProvider()
            {
                Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public TaskCompletionSource<bool> Started { get; }

            public TaskCompletionSource<bool> Release { get; }

            public async Task<GridDataPage<int>> GetPageAsync(int offset, int size, CancellationToken cancellationToken)
            {
                Started.TrySetResult(true);

                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(() => cancelled.TrySetCanceled()))
                {
                    var completed = await Task.WhenAny(Release.Task, cancelled.Task).ConfigureAwait(false);
                    await completed.ConfigureAwait(false);
                }

                return new GridDataPage<int>(offset, Enumerable.Range(offset, size).ToArray(), 1000);
            }
        }
    }
}

