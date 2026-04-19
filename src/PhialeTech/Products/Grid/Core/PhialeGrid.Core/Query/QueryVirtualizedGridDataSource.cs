using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class QueryVirtualizedGridDataSource<T> : IGridVersionedDataPageProvider<T>
    {
        private readonly IGridQueryDataProvider<T> _provider;
        private readonly int _pageSize;
        private GridQueryState _state;
        private long _version;

        public QueryVirtualizedGridDataSource(IGridQueryDataProvider<T> provider, int pageSize = 200)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _pageSize = pageSize;
            _state = new GridQueryState(Array.Empty<GridSortDescriptor>(), GridFilterGroup.EmptyAnd(), Array.Empty<GridGroupDescriptor>(), Array.Empty<GridSummaryDescriptor>());
            _version = 1L;
            LastSummary = GridSummarySet.Empty;
        }

        public event EventHandler VersionChanged;

        public event EventHandler MetadataChanged;

        public long Version => _version;

        public GridFilterGroup FilterGroup
        {
            get { return _state.FilterGroup; }
            set { ApplyState(_state.WithFilterGroup(value)); }
        }

        public IReadOnlyList<GridSortDescriptor> Sorts
        {
            get { return _state.Sorts; }
            set { ApplyState(_state.WithSorts(value)); }
        }

        public IReadOnlyList<GridGroupDescriptor> Groups
        {
            get { return _state.Groups; }
            set { ApplyState(_state.WithGroups(value)); }
        }

        public IReadOnlyList<GridSummaryDescriptor> Summaries
        {
            get { return _state.Summaries; }
            set { ApplyState(_state.WithSummaries(value)); }
        }

        public GridQueryState State => _state;

        public GridSummarySet LastSummary { get; private set; }

        public int LastTotalCount { get; private set; }

        public void ApplyState(GridQueryState state)
        {
            _state = state ?? new GridQueryState(Array.Empty<GridSortDescriptor>(), GridFilterGroup.EmptyAnd(), Array.Empty<GridGroupDescriptor>(), Array.Empty<GridSummaryDescriptor>());
            Invalidate();
        }

        public void Invalidate()
        {
            _version++;
            LastSummary = GridSummarySet.Empty;
            LastTotalCount = 0;
            var handler = VersionChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            var metadataChanged = MetadataChanged;
            if (metadataChanged != null)
            {
                metadataChanged(this, EventArgs.Empty);
            }
        }

        public Task<GridDataPage<T>> GetPageAsync(int pageStart, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetPageAsync(pageStart, _pageSize, cancellationToken);
        }

        public async Task<GridDataPage<T>> GetPageAsync(int pageStart, int size, CancellationToken cancellationToken)
        {
            if (pageStart < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageStart));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var state = _state;
            var request = new GridQueryRequest(pageStart, size, state.Sorts, state.FilterGroup, state.Groups, state.Summaries);
            var result = await _provider.QueryAsync(request, cancellationToken).ConfigureAwait(false);
            LastSummary = result.Summary;
            LastTotalCount = result.TotalCount;
            var metadataChanged = MetadataChanged;
            if (metadataChanged != null)
            {
                metadataChanged(this, EventArgs.Empty);
            }

            return new GridDataPage<T>(pageStart, result.Items, result.TotalCount);
        }
    }
}
