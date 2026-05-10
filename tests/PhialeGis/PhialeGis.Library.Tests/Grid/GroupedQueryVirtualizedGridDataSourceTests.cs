using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GroupedQueryVirtualizedGridDataSourceTests
    {
        [Test]
        public async Task GetPageAsync_ForwardsGroupedState_AndUpdatesMetadata()
        {
            var provider = new FakeGroupedQueryProvider();
            var source = new GroupedQueryVirtualizedGridDataSource<TestRow>(provider, pageSize: 25)
            {
                Sorts = new[] { new GridSortDescriptor("Name", GridSortDirection.Ascending) },
                FilterGroup = new GridFilterGroup(new[] { new GridFilterDescriptor("Active", GridFilterOperator.IsTrue) }, GridLogicalOperator.And),
                Groups = new[] { new GridGroupDescriptor("City") },
                Summaries = new[] { new GridSummaryDescriptor("Amount", GridSummaryType.Sum) },
            };

            var page = await source.GetPageAsync(50);

            Assert.That(page.Offset, Is.EqualTo(50));
            Assert.That(provider.LastRequest.Size, Is.EqualTo(25));
            Assert.That(provider.LastRequest.Offset, Is.EqualTo(50));
            Assert.That(provider.LastRequest.Sorts.Count, Is.EqualTo(1));
            Assert.That(provider.LastRequest.FilterGroup.Filters.Count, Is.EqualTo(1));
            Assert.That(provider.LastRequest.Groups.Count, Is.EqualTo(1));
            Assert.That(source.LastVisibleRowCount, Is.EqualTo(250));
            Assert.That(source.LastTotalItemCount, Is.EqualTo(180));
            Assert.That(source.LastTopLevelGroupCount, Is.EqualTo(4));
            Assert.That(source.LastSummary["Amount:Sum"], Is.EqualTo(77m));
            Assert.That(source.LastGroupIds.Count, Is.EqualTo(2));
        }

        private sealed class FakeGroupedQueryProvider : IGridGroupedQueryDataProvider<TestRow>
        {
            public GridGroupedQueryRequest LastRequest { get; private set; }

            public Task<GridGroupedQueryResult<TestRow>> QueryGroupedAsync(GridGroupedQueryRequest request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new GridGroupedQueryResult<TestRow>(
                    new[] { GridGroupFlatRow<TestRow>.CreateGroupHeader("root/City:QQ==", "City", "A", 3, 0, true) },
                    250,
                    180,
                    4,
                    new[] { "g1", "g2" },
                    new GridSummarySet(new System.Collections.Generic.Dictionary<string, object> { { "Amount:Sum", 77m } })));
            }
        }
    }
}

