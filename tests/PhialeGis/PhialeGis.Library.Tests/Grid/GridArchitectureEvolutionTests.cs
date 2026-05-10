using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Clipboard;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Export;
using PhialeGrid.Core.Hierarchy;
using PhialeGrid.Core.Input;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Core.Virtualization;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridArchitectureEvolutionTests
    {
        [Test]
        public void QueryEngine_StreamedPath_EnumeratesSourceOnce()
        {
            var source = new CountingEnumerable<TestRow>(Enumerable.Range(0, 100).Select(i => new TestRow
            {
                Id = i.ToString(CultureInfo.InvariantCulture),
                Age = i,
                Name = "N" + i.ToString(CultureInfo.InvariantCulture),
            }));

            var engine = new GridQueryEngine<TestRow>(new DelegateGridRowAccessor<TestRow>((row, col) =>
            {
                if (col == "Age")
                {
                    return row.Age;
                }

                return row.Name;
            }));

            var result = engine.Execute(
                source,
                new GridQueryRequest(
                    10,
                    5,
                    Array.Empty<GridSortDescriptor>(),
                    new GridFilterGroup(new[] { new GridFilterDescriptor("Age", GridFilterOperator.GreaterThan, 4) }, GridLogicalOperator.And),
                    Array.Empty<GridGroupDescriptor>(),
                    new[] { new GridSummaryDescriptor("Age", GridSummaryType.Sum) }));

            Assert.That(source.EnumerationCount, Is.EqualTo(1));
            Assert.That(result.Items.Count, Is.EqualTo(5));
            Assert.That(result.TotalCount, Is.EqualTo(95));
        }

        [Test]
        public async Task QuerySourceVersioning_InvalidatesVirtualizedCache()
        {
            var provider = new FakeVersionedQueryProvider();
            var querySource = new QueryVirtualizedGridDataSource<int>(provider, pageSize: 10);
            var cache = new VirtualizedGridDataSource<int>(querySource, pageSize: 10, maxCachedPages: 4, prefetchPageRadius: 0);

            var first = await cache.GetItemAsync(0).ConfigureAwait(false);
            querySource.FilterGroup = new GridFilterGroup(new[] { new GridFilterDescriptor("Marker", GridFilterOperator.Equals, "x") }, GridLogicalOperator.And);
            var second = await cache.GetItemAsync(0).ConfigureAwait(false);

            Assert.That(first, Is.EqualTo(0));
            Assert.That(second, Is.EqualTo(1000));
            Assert.That(provider.RequestCount, Is.EqualTo(2));
            Assert.That(querySource.Version, Is.GreaterThan(1L));
        }

        [Test]
        public void Viewport_SupportsVariableRowsFrozenColumnsAndBands()
        {
            var viewport = new GridViewport(
                horizontalOffset: 60,
                verticalOffset: 35,
                viewportWidth: 200,
                viewportHeight: 70,
                rowHeights: new[] { 10d, 20d, 30d, 40d, 50d },
                columnWidths: new[] { 80d, 90d, 100d, 110d },
                frozenColumnCount: 1,
                headerBandCount: 2);

            var rows = viewport.CalculateVisibleRows(5, overscan: 0);
            var frozen = viewport.CalculateFrozenColumns();
            var scrollable = viewport.CalculateScrollableColumns(overscan: 0);
            var bands = viewport.CalculateHeaderBands();

            Assert.That(rows.Start, Is.EqualTo(2));
            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(frozen.Start, Is.EqualTo(0));
            Assert.That(frozen.Length, Is.EqualTo(1));
            Assert.That(scrollable.Start, Is.EqualTo(1));
            Assert.That(scrollable.Length, Is.EqualTo(2));
            Assert.That(bands.Length, Is.EqualTo(2));
        }

        [Test]
        public void LayoutState_ExposesImmutableSnapshots()
        {
            var state = new GridLayoutState(new[]
            {
                new GridColumnDefinition("Id", "Id", width: 100, displayIndex: 0),
                new GridColumnDefinition("Name", "Name", width: 120, displayIndex: 1),
            });

            var snapshotBefore = state.Columns.Single(c => c.Id == "Name");
            state.ResizeColumn("Name", 240);
            var snapshotAfter = state.Columns.Single(c => c.Id == "Name");

            Assert.That(snapshotBefore.Width, Is.EqualTo(120));
            Assert.That(snapshotAfter.Width, Is.EqualTo(240));
        }

        [Test]
        public void StateCodec_UsesVersionedPayload_AndRejectsLegacyPayloads()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Age", "Age", width: 120, displayIndex: 0, valueType: typeof(int)),
            };
            var snapshot = new GridStateSnapshot(
                new GridLayoutSnapshot(columns),
                Array.Empty<GridSortDescriptor>(),
                GridFilterGroup.EmptyAnd(),
                Array.Empty<GridGroupDescriptor>(),
                Array.Empty<GridSummaryDescriptor>());

            var encoded = GridStateCodec.Encode(snapshot);
            var legacyV2 = "v2|QWdl,0,120,1,0||||";

            Assert.That(encoded.StartsWith("v7|", StringComparison.Ordinal), Is.True);
            Assert.That(() => GridStateCodec.Decode(legacyV2, columns), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void QuerySchema_NormalizesTypedValuesForFiltersAndSummaries()
        {
            var columns = new[]
            {
                new GridColumnDefinition("Age", "Age", displayIndex: 0, valueType: typeof(int)),
                new GridColumnDefinition("Amount", "Amount", displayIndex: 1, valueType: typeof(decimal)),
            };
            var engine = new GridQueryEngine<TestRow>(
                new DelegateGridRowAccessor<TestRow>((row, col) =>
                {
                    if (col == "Age")
                    {
                        return row.Age;
                    }

                    return row.Amount;
                }),
                GridQuerySchema.FromColumns(columns));

            var result = engine.Execute(
                new[]
                {
                    new TestRow { Id = "1", Age = 30, Amount = 10m },
                    new TestRow { Id = "2", Age = 40, Amount = 20m },
                },
                new GridQueryRequest(
                    0,
                    10,
                    Array.Empty<GridSortDescriptor>(),
                    new GridFilterGroup(new[] { new GridFilterDescriptor("Age", GridFilterOperator.GreaterThan, "35") }, GridLogicalOperator.And),
                    Array.Empty<GridGroupDescriptor>(),
                    new[] { new GridSummaryDescriptor("Amount", GridSummaryType.Sum, typeof(decimal)) }));

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Summary.GetValue<decimal>("Amount:Sum"), Is.EqualTo(20m));
        }

        [Test]
        public void GridEvents_SupportCancellationAfterEventsAndExceptionPolicy()
        {
            var eventsHub = new GridEvents();
            var handlerExceptions = 0;
            var afterCalls = 0;

            eventsHub.ExceptionPolicy = GridEventExceptionPolicy.CaptureAndContinue;
            eventsHub.HandlerException += (_, __) => handlerExceptions++;
            eventsHub.Sorting += (_, args) => args.Cancel = true;
            eventsHub.Filtering += (_, __) => throw new InvalidOperationException("boom");
            eventsHub.Filtered += (_, __) => afterCalls++;

            var sortAllowed = eventsHub.RaiseSorting(new[] { new GridSortDescriptor("Name", GridSortDirection.Ascending) });
            var filterAllowed = eventsHub.RaiseFiltering(GridFilterGroup.EmptyAnd());
            eventsHub.RaiseFiltered(GridFilterGroup.EmptyAnd());

            Assert.That(sortAllowed, Is.False);
            Assert.That(filterAllowed, Is.True);
            Assert.That(handlerExceptions, Is.EqualTo(1));
            Assert.That(afterCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task EditSession_TracksCellDiffsAndConcurrencyConflicts()
        {
            var row = new TestRow { Id = "1", Name = "Alice", City = "v1", Age = 30 };
            var session = new GridEditSession<TestRow>(CreateRowEditor(), x => x.Id, null, x => x.City);

            session.BeginEdit(row);
            session.SetCellValue(row, "Name", "Alice 2");

            var changes = session.GetCellChanges(row.Id);
            var commit = await session.CommitAsync(
                (id, _) => Task.FromResult(new TestRow { Id = id, Name = "Alice server", City = "v2", Age = 30 }),
                CancellationToken.None).ConfigureAwait(false);

            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].ColumnId, Is.EqualTo("Name"));
            Assert.That(commit.Single().HasConflict, Is.True);
            Assert.That(commit.Single().Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ClipboardAndCsv_SupportBusinessFormatOptions()
        {
            var clipboardText = GridClipboardCodec.Encode(
                new[]
                {
                    (IReadOnlyList<string>)new[] { "A", "B" },
                    (IReadOnlyList<string>)new[] { "C", "D" },
                },
                new GridClipboardOptions { Delimiter = ';', LineEnding = "\r\n" });

            var csv = GridCsvExporter.Export(
                new[] { new TestRow { Id = "1", Name = "Alice" } },
                new[] { "Id", "Name" },
                (row, col) => col == "Id" ? (object)row.Id : row.Name,
                new GridCsvOptions { Delimiter = ';', LineEnding = "\r\n", IncludeHeader = true });
            var imported = GridCsvImporter.Import(csv, new GridCsvOptions { Delimiter = ';', HasHeaderOnImport = true });
            var clipboardRows = GridClipboardCodec.Decode(clipboardText, new GridClipboardOptions { Delimiter = ';', LineEnding = "\r\n" });

            Assert.That(clipboardText.Contains("\r\n"), Is.True);
            Assert.That(clipboardRows[0][1], Is.EqualTo("B"));
            Assert.That(imported[0]["Name"], Is.EqualTo("Alice"));
            Assert.That(csv.Split(new[] { "\r\n" }, StringSplitOptions.None)[0], Is.EqualTo("Id;Name"));
        }

        [Test]
        public async Task GroupingAndHierarchy_HaveStableExpansionStateAndPaging()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", City = "Warsaw", Name = "A" },
                new TestRow { Id = "2", City = "Berlin", Name = "B" },
                new TestRow { Id = "3", City = "Warsaw", Name = "C" },
            };

            var engine = new GridQueryEngine<TestRow>(new DelegateGridRowAccessor<TestRow>((row, col) => col == "City" ? (object)row.City : row.Name));
            var expansion = new GridGroupExpansionState();
            var firstBuild = engine.BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);
            expansion.SetExpanded(firstBuild[0].Id, false);
            var secondBuild = engine.BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);

            Assert.That(firstBuild[0].Id, Is.EqualTo(secondBuild[0].Id));
            Assert.That(secondBuild[0].IsExpanded, Is.False);

            var hierarchyState = new GridHierarchyExpansionState();
            var hierarchy = new GridHierarchyController<string>(new FakePagingHierarchyProvider(), hierarchyState, pageSize: 2);
            var root = new GridHierarchyNode<string>("root", "root", true);

            await hierarchy.ExpandAsync(root).ConfigureAwait(false);
            Assert.That(root.Children.Count, Is.EqualTo(2));
            Assert.That(root.HasMoreChildren, Is.True);

            await hierarchy.LoadNextChildrenPageAsync(root).ConfigureAwait(false);
            Assert.That(root.Children.Count, Is.EqualTo(3));

            hierarchy.Collapse(root);
            var flat = hierarchy.Flatten(new[] { root });
            Assert.That(flat.Count, Is.EqualTo(1));
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

        private sealed class CountingEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _source;

            public CountingEnumerable(IEnumerable<T> source)
            {
                _source = source;
            }

            public int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                return _source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class FakeVersionedQueryProvider : IGridQueryDataProvider<int>
        {
            public int RequestCount { get; private set; }

            public Task<GridQueryResult<int>> QueryAsync(GridQueryRequest request, CancellationToken cancellationToken)
            {
                RequestCount++;
                var marker = request.FilterGroup.Filters.Count == 0 ? 0 : 1000;
                var items = Enumerable.Range(request.Offset, request.Size).Select(x => x + marker).ToArray();
                return Task.FromResult(new GridQueryResult<int>(items, 5000, Array.Empty<GridGroupNode<int>>(), GridSummarySet.Empty));
            }
        }

        private sealed class FakePagingHierarchyProvider : IGridHierarchyPagingProvider<string>
        {
            public Task<IReadOnlyList<GridHierarchyNode<string>>> LoadChildrenAsync(GridHierarchyNode<string> parent, CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyList<GridHierarchyNode<string>>>(new[]
                {
                    new GridHierarchyNode<string>("c1", "c1", false, parent.PathId),
                    new GridHierarchyNode<string>("c2", "c2", false, parent.PathId),
                    new GridHierarchyNode<string>("c3", "c3", false, parent.PathId),
                });
            }

            public Task<GridHierarchyPage<string>> LoadChildrenPageAsync(GridHierarchyNode<string> parent, int offset, int size, CancellationToken cancellationToken)
            {
                var all = new[]
                {
                    new GridHierarchyNode<string>("c1", "c1", false, parent.PathId),
                    new GridHierarchyNode<string>("c2", "c2", false, parent.PathId),
                    new GridHierarchyNode<string>("c3", "c3", false, parent.PathId),
                };
                var page = all.Skip(offset).Take(size).ToArray();
                var hasMore = offset + size < all.Length;
                return Task.FromResult(new GridHierarchyPage<string>(page, hasMore));
            }
        }
    }
}

