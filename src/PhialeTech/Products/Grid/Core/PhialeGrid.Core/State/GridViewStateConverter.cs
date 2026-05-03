using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.State
{
    public static class GridViewStateConverter
    {
        public static GridViewState FromSnapshot(GridStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new GridViewState
            {
                Version = 4,
                Columns = snapshot.Layout.Columns
                    .Select(column => new GridViewColumnState
                    {
                        ColumnId = column.Id,
                        DisplayIndex = column.DisplayIndex,
                        Width = column.Width,
                        IsVisible = column.IsVisible,
                        IsFrozen = column.IsFrozen,
                    })
                    .ToList(),
                Sorts = snapshot.Sorts
                    .Select(sort => new GridViewSortState
                    {
                        ColumnId = sort.ColumnId,
                        Direction = sort.Direction,
                    })
                    .ToList(),
                Filters = new GridViewFilterGroupState
                {
                    LogicalOperator = snapshot.Filters.LogicalOperator,
                    Filters = snapshot.Filters.Filters
                        .Select(filter => new GridViewFilterState
                        {
                            ColumnId = filter.ColumnId,
                            Operator = filter.Operator,
                            HasValue = filter.Value != null,
                            ValueText = filter.Value == null ? null : Convert.ToString(filter.Value, CultureInfo.InvariantCulture),
                            HasSecondValue = filter.SecondValue != null,
                            SecondValueText = filter.SecondValue == null ? null : Convert.ToString(filter.SecondValue, CultureInfo.InvariantCulture),
                        })
                        .ToList(),
                },
                Groups = snapshot.Groups
                    .Select(group => new GridViewGroupState
                    {
                        ColumnId = group.ColumnId,
                        Direction = group.Direction,
                    })
                    .ToList(),
                Summaries = snapshot.Summaries
                    .Select(summary => new GridViewSummaryState
                    {
                        ColumnId = summary.ColumnId,
                        Type = summary.Type,
                    })
                    .ToList(),
                RegionLayout = snapshot.RegionLayout.Regions
                    .Select(region => new GridViewRegionState
                    {
                        RegionKind = region.RegionKind,
                        State = region.State,
                        Size = region.Size,
                        IsActive = region.IsActive,
                        PlacementOverride = region.PlacementOverride,
                    })
                    .ToList(),
                GlobalSearchText = snapshot.GlobalSearchText ?? string.Empty,
                SelectCurrentRow = snapshot.SelectCurrentRow,
                MultiSelect = snapshot.MultiSelect,
                ShowRowNumbers = snapshot.ShowRowNumbers,
                RowNumberingMode = snapshot.RowNumberingMode,
            };
        }

        public static GridStateSnapshot ToSnapshot(GridViewState state, IReadOnlyList<GridColumnDefinition> baselineColumns)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (baselineColumns == null)
            {
                throw new ArgumentNullException(nameof(baselineColumns));
            }

            var baselineMap = baselineColumns.ToDictionary(column => column.Id, StringComparer.OrdinalIgnoreCase);
            var projectedColumns = (state.Columns ?? new List<GridViewColumnState>())
                .Where(column => !string.IsNullOrWhiteSpace(column.ColumnId) && baselineMap.ContainsKey(column.ColumnId))
                .Select(column =>
                {
                    var baseline = baselineMap[column.ColumnId];
                    return baseline
                        .WithDisplayIndex(column.DisplayIndex)
                        .WithWidth(column.Width <= 0d ? baseline.Width : column.Width)
                        .WithVisibility(column.IsVisible)
                        .WithFrozen(column.IsFrozen);
                })
                .ToArray();

            var projectedColumnIds = new HashSet<string>(
                projectedColumns.Select(column => column.Id),
                StringComparer.OrdinalIgnoreCase);
            var columns = projectedColumns
                .Concat(baselineColumns.Where(column => !projectedColumnIds.Contains(column.Id)))
                .OrderBy(column => column.DisplayIndex)
                .ToArray();

            if (columns.Length == 0)
            {
                columns = baselineColumns.ToArray();
            }

            var sorts = (state.Sorts ?? new List<GridViewSortState>())
                .Where(sort => !string.IsNullOrWhiteSpace(sort.ColumnId))
                .Select(sort => new GridSortDescriptor(sort.ColumnId, sort.Direction))
                .ToArray();

            var filters = (state.Filters?.Filters ?? new List<GridViewFilterState>())
                .Where(filter => !string.IsNullOrWhiteSpace(filter.ColumnId))
                .Select(filter => new GridFilterDescriptor(
                    filter.ColumnId,
                    filter.Operator,
                    filter.HasValue ? (object)filter.ValueText : null,
                    filter.HasSecondValue ? (object)filter.SecondValueText : null))
                .ToArray();

            var filterGroup = new GridFilterGroup(filters, state.Filters?.LogicalOperator ?? GridLogicalOperator.And);

            var groups = (state.Groups ?? new List<GridViewGroupState>())
                .Where(group => !string.IsNullOrWhiteSpace(group.ColumnId))
                .Select(group => new GridGroupDescriptor(group.ColumnId, group.Direction))
                .ToArray();

            var summaries = (state.Summaries ?? new List<GridViewSummaryState>())
                .Where(summary => !string.IsNullOrWhiteSpace(summary.ColumnId))
                .Select(summary => new GridSummaryDescriptor(summary.ColumnId, summary.Type))
                .ToArray();

            var regionLayout = new GridRegionLayoutSnapshot((state.RegionLayout ?? new List<GridViewRegionState>())
                .Select(region => new GridRegionLayoutState(
                    region.RegionKind,
                    region.State,
                    region.Size,
                    region.IsActive,
                    region.PlacementOverride))
                .ToArray());

            return new GridStateSnapshot(
                new GridLayoutSnapshot(columns),
                sorts,
                filterGroup,
                groups,
                summaries,
                regionLayout,
                state.GlobalSearchText ?? string.Empty,
                state.SelectCurrentRow,
                state.MultiSelect,
                state.ShowRowNumbers,
                state.RowNumberingMode);
        }
    }
}
