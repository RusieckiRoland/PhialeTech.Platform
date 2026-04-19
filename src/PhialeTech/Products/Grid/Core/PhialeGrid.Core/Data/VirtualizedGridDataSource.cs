using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Data
{
    public sealed class VirtualizedGridDataSource<T>
    {
        private readonly IGridDataPageProvider<T> _provider;
        private readonly int _pageSize;
        private readonly int _prefetchPageRadius;
        private readonly int _maxCachedPages;

        private readonly Dictionary<int, GridDataPage<T>> _pageCache = new Dictionary<int, GridDataPage<T>>();
        private readonly LinkedList<int> _lru = new LinkedList<int>();
        private readonly Dictionary<int, LinkedListNode<int>> _lruNodes = new Dictionary<int, LinkedListNode<int>>();
        private readonly Dictionary<int, Task<GridDataPage<T>>> _inFlight = new Dictionary<int, Task<GridDataPage<T>>>();
        private readonly object _gate = new object();
        private readonly IGridVersionedDataSource _versionedProvider;

        private int? _knownCount;
        private long _observedVersion;

        public VirtualizedGridDataSource(IGridDataPageProvider<T> provider, int pageSize = 200, int maxCachedPages = 24, int prefetchPageRadius = 1)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            if (maxCachedPages <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCachedPages));
            }

            if (prefetchPageRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefetchPageRadius));
            }

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _pageSize = pageSize;
            _maxCachedPages = maxCachedPages;
            _prefetchPageRadius = prefetchPageRadius;
            _versionedProvider = provider as IGridVersionedDataSource;
            if (_versionedProvider != null)
            {
                _observedVersion = _versionedProvider.Version;
                _versionedProvider.VersionChanged += HandleProviderVersionChanged;
            }
        }

        public int PageSize => _pageSize;

        public int MaxCachedPages => _maxCachedPages;

        public int PrefetchPageRadius => _prefetchPageRadius;

        public int PageRequestCount { get; private set; }

        public int CacheHitCount { get; private set; }

        public void Invalidate()
        {
            lock (_gate)
            {
                ClearCacheUnsafe();
            }
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_knownCount.HasValue)
            {
                return _knownCount.Value;
            }

            var page = await GetOrLoadPageAsync(0, cancellationToken).ConfigureAwait(false);
            return page.TotalCount;
        }

        public async Task<T> GetItemAsync(int index, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var pageStart = ToPageStart(index);
            var page = await GetOrLoadPageAsync(pageStart, cancellationToken).ConfigureAwait(false);

            if (index >= page.TotalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is outside of source range.");
            }

            var withinPage = index - pageStart;
            if (withinPage >= page.Items.Count)
            {
                throw new InvalidOperationException("Provider returned an incomplete page for requested index.");
            }

            await PrefetchAroundAsync(pageStart, cancellationToken).ConfigureAwait(false);
            return page.Items[withinPage];
        }

        public async Task<IReadOnlyList<T>> GetVisibleWindowAsync(int startIndex, int size, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var list = new List<T>(size);
            var endExclusive = startIndex + size;
            var firstPage = ToPageStart(startIndex);
            var lastPage = ToPageStart(endExclusive - 1);

            for (var pageStart = firstPage; pageStart <= lastPage; pageStart += _pageSize)
            {
                var page = await GetOrLoadPageAsync(pageStart, cancellationToken).ConfigureAwait(false);
                var from = Math.Max(startIndex, pageStart);
                var to = Math.Min(endExclusive, pageStart + page.Items.Count);

                for (var i = from; i < to; i++)
                {
                    list.Add(page.Items[i - pageStart]);
                }

                await PrefetchAroundAsync(pageStart, cancellationToken).ConfigureAwait(false);
            }

            return list;
        }

        public IReadOnlyList<int> GetCachedPageStarts()
        {
            lock (_gate)
            {
                return _pageCache.Keys.OrderBy(x => x).ToArray();
            }
        }

        private int ToPageStart(int index)
        {
            return (index / _pageSize) * _pageSize;
        }

        private async Task<GridDataPage<T>> GetOrLoadPageAsync(int pageStart, CancellationToken cancellationToken)
        {
            Task<GridDataPage<T>> fetchTask;

            lock (_gate)
            {
                EnsureProviderVersionUpToDateUnsafe();
                var fetchVersion = _observedVersion;
                GridDataPage<T> cached;
                if (_pageCache.TryGetValue(pageStart, out cached))
                {
                    CacheHitCount++;
                    TouchLru(pageStart);
                    return cached;
                }

                if (!_inFlight.TryGetValue(pageStart, out fetchTask))
                {
                    fetchTask = FetchAndCacheAsync(pageStart, fetchVersion, CancellationToken.None);
                    _inFlight[pageStart] = fetchTask;
                }
            }

            return await AwaitWithCallerCancellationAsync(fetchTask, cancellationToken).ConfigureAwait(false);
        }

        private async Task<GridDataPage<T>> FetchAndCacheAsync(int pageStart, long fetchVersion, CancellationToken cancellationToken)
        {
            GridDataPage<T> page;
            try
            {
                PageRequestCount++;
                page = await _provider.GetPageAsync(pageStart, _pageSize, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (_gate)
                {
                    _inFlight.Remove(pageStart);
                }
            }

            if (page == null)
            {
                throw new InvalidOperationException("Provider returned null page.");
            }

            lock (_gate)
            {
                if (_versionedProvider != null && fetchVersion != _observedVersion)
                {
                    return page;
                }

                _knownCount = page.TotalCount;
                _pageCache[pageStart] = page;
                TouchLru(pageStart);
                TrimCacheIfNeeded();
            }

            return page;
        }

        private async Task PrefetchAroundAsync(int centerPageStart, CancellationToken cancellationToken)
        {
            if (_prefetchPageRadius == 0)
            {
                return;
            }

            var tasks = new List<Task>(_prefetchPageRadius * 2);
            var knownCount = _knownCount;
            for (var step = 1; step <= _prefetchPageRadius; step++)
            {
                var prev = centerPageStart - (step * _pageSize);
                if (prev >= 0)
                {
                    tasks.Add(GetOrLoadPageAsync(prev, cancellationToken));
                }

                var next = centerPageStart + (step * _pageSize);
                if (!knownCount.HasValue || next < knownCount.Value)
                {
                    tasks.Add(GetOrLoadPageAsync(next, cancellationToken));
                }
            }

            if (tasks.Count == 0)
            {
                return;
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void TouchLru(int pageStart)
        {
            LinkedListNode<int> node;
            if (_lruNodes.TryGetValue(pageStart, out node))
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
                return;
            }

            var newNode = _lru.AddFirst(pageStart);
            _lruNodes[pageStart] = newNode;
        }

        private void TrimCacheIfNeeded()
        {
            while (_pageCache.Count > _maxCachedPages)
            {
                var last = _lru.Last;
                if (last == null)
                {
                    return;
                }

                var pageStart = last.Value;
                _lru.RemoveLast();
                _lruNodes.Remove(pageStart);
                _pageCache.Remove(pageStart);
            }
        }

        private void HandleProviderVersionChanged(object sender, EventArgs e)
        {
            lock (_gate)
            {
                if (_versionedProvider != null)
                {
                    _observedVersion = _versionedProvider.Version;
                }

                ClearCacheUnsafe();
            }
        }

        private void EnsureProviderVersionUpToDateUnsafe()
        {
            if (_versionedProvider == null)
            {
                return;
            }

            if (_observedVersion == _versionedProvider.Version)
            {
                return;
            }

            _observedVersion = _versionedProvider.Version;
            ClearCacheUnsafe();
        }

        private void ClearCacheUnsafe()
        {
            _pageCache.Clear();
            _lru.Clear();
            _lruNodes.Clear();
            _inFlight.Clear();
            _knownCount = null;
        }

        private static async Task<TValue> AwaitWithCallerCancellationAsync<TValue>(Task<TValue> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return await task.ConfigureAwait(false);
            }

            var cancellation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(true), cancellation))
            {
                if (await Task.WhenAny(task, cancellation.Task).ConfigureAwait(false) != task)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }
}
