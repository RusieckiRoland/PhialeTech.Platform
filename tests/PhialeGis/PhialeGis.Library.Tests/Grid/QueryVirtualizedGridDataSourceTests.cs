using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class QueryVirtualizedGridDataSourceTests
    {
        [Test]
        public async Task GetPageAsync_ForwardsRequestStateToProvider()
        {
            var provider = new FakeQueryProvider();
            var source = new QueryVirtualizedGridDataSource<TestRow>(provider, pageSize: 50)
            {
                Sorts = new[] { new GridSortDescriptor("Name", GridSortDirection.Ascending) },
                FilterGroup = new GridFilterGroup(new[] { new GridFilterDescriptor("Active", GridFilterOperator.IsTrue) }, GridLogicalOperator.And),
                Groups = new[] { new GridGroupDescriptor("City") },
            };

            var page = await source.GetPageAsync(100);

            Assert.That(page.Offset, Is.EqualTo(100));
            Assert.That(provider.LastRequest.Size, Is.EqualTo(50));
            Assert.That(provider.LastRequest.Offset, Is.EqualTo(100));
            Assert.That(provider.LastRequest.Sorts.Count, Is.EqualTo(1));
            Assert.That(provider.LastRequest.FilterGroup.Filters.Count, Is.EqualTo(1));
            Assert.That(provider.LastRequest.Groups.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetPageAsync_UpdatesMetadata()
        {
            var provider = new FakeQueryProvider();
            var source = new QueryVirtualizedGridDataSource<TestRow>(provider, pageSize: 50)
            {
                Summaries = new[] { new PhialeGrid.Core.Summaries.GridSummaryDescriptor("Amount", PhialeGrid.Core.Summaries.GridSummaryType.Sum) },
            };

            await source.GetPageAsync(0);

            Assert.That(source.LastTotalCount, Is.EqualTo(1));
            Assert.That(source.LastSummary["Amount:Sum"], Is.EqualTo(20m));
        }

        private sealed class FakeQueryProvider : IGridQueryDataProvider<TestRow>
        {
            public GridQueryRequest LastRequest { get; private set; }

            public Task<GridQueryResult<TestRow>> QueryAsync(GridQueryRequest request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new GridQueryResult<TestRow>(
                    new[] { new TestRow { Id = "x" } },
                    1,
                    System.Array.Empty<GridGroupNode<TestRow>>(),
                    new PhialeGrid.Core.Summaries.GridSummarySet(new System.Collections.Generic.Dictionary<string, object> { { "Amount:Sum", 20m } })));
            }
        }
    }
}

