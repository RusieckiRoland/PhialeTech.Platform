using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GroupedQueryVirtualizedGridDataSource<T> : IGridVersionedDataPageProvider<GridGroupFlatRow<T>>
    {
        private readonly IGridGroupedQueryDataProvider<T> _provider;
        private readonly int _pageSize;
        private GridGroupedQueryState _state;
        private long _version;

        public GroupedQueryVirtualizedGridDataSource(IGridGroupedQueryDataProvider<T> provider, int pageSize = 200)
        {
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _pageSize = pageSize;
            _state = new GridGroupedQueryState(
                Array.Empty<GridSortDescriptor>(),
                GridFilterGroup.EmptyAnd(),
                Array.Empty<GridGroupDescriptor>(),
                Array.Empty<GridSummaryDescriptor>(),
                new GridGroupExpansionState());
            _version = 1L;
            LastSummary = GridSummarySet.Empty;
            LastGroupIds = Array.Empty<string>();
        }

        public event EventHandler VersionChanged;

        public event EventHandler MetadataChanged;

        public long Version => _version;

        public int PageSize => _pageSize;

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

        public GridGroupExpansionState ExpansionState
        {
            get { return _state.ExpansionState; }
            set { ApplyState(_state.WithExpansionState(value)); }
        }

        public GridGroupedQueryState State => _state;

        public GridSummarySet LastSummary { get; private set; }

        public int LastVisibleRowCount { get; private set; }

        public int LastTotalItemCount { get; private set; }

        public int LastTopLevelGroupCount { get; private set; }

        public IReadOnlyList<string> LastGroupIds { get; private set; }

        public void ApplyState(GridGroupedQueryState state)
        {
            _state = state ?? new GridGroupedQueryState(
                Array.Empty<GridSortDescriptor>(),
                GridFilterGroup.EmptyAnd(),
                Array.Empty<GridGroupDescriptor>(),
                Array.Empty<GridSummaryDescriptor>(),
                new GridGroupExpansionState());
            Invalidate();
        }

        public void Invalidate()
        {
            _version++;
            LastSummary = GridSummarySet.Empty;
            LastVisibleRowCount = 0;
            LastTotalItemCount = 0;
            LastTopLevelGroupCount = 0;
            LastGroupIds = Array.Empty<string>();
            var versionChanged = VersionChanged;
            if (versionChanged != null)
            {
                versionChanged(this, EventArgs.Empty);
            }

            var metadataChanged = MetadataChanged;
            if (metadataChanged != null)
            {
                metadataChanged(this, EventArgs.Empty);
            }
        }

        public Task<GridDataPage<GridGroupFlatRow<T>>> GetPageAsync(int pageStart, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetPageAsync(pageStart, _pageSize, cancellationToken);
        }

        public async Task<GridDataPage<GridGroupFlatRow<T>>> GetPageAsync(int pageStart, int size, CancellationToken cancellationToken)
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
            var request = new GridGroupedQueryRequest(
                pageStart,
                size,
                state.Sorts,
                state.FilterGroup,
                state.Groups,
                state.Summaries,
                state.ExpansionState);
            var result = await _provider.QueryGroupedAsync(request, cancellationToken).ConfigureAwait(false);

            LastSummary = result.Summary;
            LastVisibleRowCount = result.VisibleRowCount;
            LastTotalItemCount = result.TotalItemCount;
            LastTopLevelGroupCount = result.TopLevelGroupCount;
            LastGroupIds = result.GroupIds;
            var metadataChanged = MetadataChanged;
            if (metadataChanged != null)
            {
                metadataChanged(this, EventArgs.Empty);
            }

            return new GridDataPage<GridGroupFlatRow<T>>(pageStart, result.Rows, result.VisibleRowCount);
        }
    }
}
