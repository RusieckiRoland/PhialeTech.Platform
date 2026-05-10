using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PhialeGrid.Core.Data;

namespace PhialeGis.Library.Tests.Grid
{
    public class VirtualizedGridDataSourceTests
    {
        [Test]
        public async Task GetItemAsync_UsesCacheWithinSamePage()
        {
            var provider = new FakePageProvider(1000);
            var source = new VirtualizedGridDataSource<int>(provider, pageSize: 10, maxCachedPages: 8, prefetchPageRadius: 0);

            var first = await source.GetItemAsync(5);
            var second = await source.GetItemAsync(8);

            Assert.That(first, Is.EqualTo(5));
            Assert.That(second, Is.EqualTo(8));
            Assert.That(provider.Requests, Is.EqualTo(1));
            Assert.That(source.CacheHitCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task GetVisibleWindowAsync_PrefetchesAdjacentPages()
        {
            var provider = new FakePageProvider(1000);
            var source = new VirtualizedGridDataSource<int>(provider, pageSize: 10, maxCachedPages: 8, prefetchPageRadius: 1);

            var window = await source.GetVisibleWindowAsync(15, 5);

            Assert.That(window.Count, Is.EqualTo(5));
            Assert.That(window[0], Is.EqualTo(15));
            CollectionAssert.IsSupersetOf(source.GetCachedPageStarts(), new[] { 10, 0, 20 });
        }

        [Test]
        public async Task Cache_EvictsLeastRecentlyUsedPage_WhenLimitExceeded()
        {
            var provider = new FakePageProvider(1000);
            var source = new VirtualizedGridDataSource<int>(provider, pageSize: 10, maxCachedPages: 2, prefetchPageRadius: 0);

            await source.GetItemAsync(1);   // page 0
            await source.GetItemAsync(11);  // page 10
            await source.GetItemAsync(21);  // page 20, evicts page 0

            var cached = source.GetCachedPageStarts();
            CollectionAssert.AreEquivalent(new[] { 10, 20 }, cached);
        }

        [Test]
        public void GetItemAsync_ThrowsForIndexOutsideTotalCount()
        {
            var provider = new FakePageProvider(50);
            var source = new VirtualizedGridDataSource<int>(provider, pageSize: 10, maxCachedPages: 4, prefetchPageRadius: 0);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await source.GetItemAsync(75));
        }

        private sealed class FakePageProvider : IGridDataPageProvider<int>
        {
            private readonly int _totalCount;

            public FakePageProvider(int totalCount)
            {
                _totalCount = totalCount;
            }

            public int Requests { get; private set; }

            public Task<GridDataPage<int>> GetPageAsync(int offset, int size, CancellationToken cancellationToken)
            {
                Requests++;

                var count = Math.Max(0, Math.Min(size, _totalCount - offset));
                var items = Enumerable.Range(offset, count).ToArray();
                return Task.FromResult(new GridDataPage<int>(offset, items, _totalCount));
            }
        }
    }
}

