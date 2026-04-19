using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhialeGrid.Core.Summaries;

namespace PhialeGrid.Core.Query
{
    public sealed class GridQueryEngine<T>
    {
        private readonly IGridRowAccessor<T> _accessor;
        private readonly GridQuerySchema _schema;

        public GridQueryEngine(IGridRowAccessor<T> accessor, GridQuerySchema schema = null)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            _schema = schema;
        }

        public GridQueryResult<T> Execute(IReadOnlyList<T> source, GridQueryRequest request)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Execute((IEnumerable<T>)source, request);
        }

        public GridQueryResult<T> Execute(IEnumerable<T> source, GridQueryRequest request)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Groups.Count == 0 && request.Sorts.Count == 0)
            {
                return ExecuteStreaming(source, request);
            }

            return ExecuteMaterialized(source, request);
        }

        public GridGroupedQueryResult<T> ExecuteGroupedWindow(IReadOnlyList<T> source, GridGroupedQueryRequest request)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ExecuteGroupedWindow((IEnumerable<T>)source, request);
        }

        public GridGroupedQueryResult<T> ExecuteGroupedWindow(IEnumerable<T> source, GridGroupedQueryRequest request)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var effectiveSorts = request.Groups.Count == 0
                ? request.Sorts
                : BuildEffectiveGroupSorts(request.Sorts, request.Groups);
            var filterProjectionPlan = ProjectionPlan.CreateFilterPlan(request.FilterGroup);
            var rowProjectionPlan = ProjectionPlan.CreateSortGroupPlan(effectiveSorts, request.Groups);
            var filterPlan = FilterExecutionPlan.Create(request.FilterGroup, filterProjectionPlan, _schema);
            var sortInstructions = SortInstruction.Create(effectiveSorts, rowProjectionPlan);
            var groupInstructions = GroupInstruction.Create(request.Groups, rowProjectionPlan);
            var summaryInstructions = SummaryInstruction.Create(request.Summaries, rowProjectionPlan);
            var materialized = MaterializeFilteredRows(source, filterProjectionPlan, rowProjectionPlan, filterPlan, sortInstructions, summaryInstructions);
            if (request.Groups.Count == 0)
            {
                var rows = materialized.Rows
                    .Skip(request.Offset)
                    .Take(request.Size)
                    .Select(row => GridGroupFlatRow<T>.CreateDataRow(row.Item, 0))
                    .ToArray();

                return new GridGroupedQueryResult<T>(
                    rows,
                    materialized.Rows.Length,
                    materialized.Rows.Length,
                    0,
                    Array.Empty<string>(),
                    materialized.Summary);
            }

            var groupIds = new List<string>();
            var groups = BuildGroupRanges(materialized.Rows, groupInstructions, 0, 0, materialized.Rows.Length, null, request.ExpansionState, groupIds);
            var rowsWindow = BuildGroupedWindow(groups, materialized.Rows, request.Offset, request.Size);
            var visibleRowCount = groups.Sum(group => group.VisibleRowCount);

            return new GridGroupedQueryResult<T>(
                rowsWindow,
                visibleRowCount,
                materialized.Rows.Length,
                groups.Count,
                groupIds.ToArray(),
                materialized.Summary);
        }

        private GridQueryResult<T> ExecuteStreaming(IEnumerable<T> source, GridQueryRequest request)
        {
            var projectionPlan = ProjectionPlan.CreateFilterPlan(request.FilterGroup);
            var filterPlan = FilterExecutionPlan.Create(request.FilterGroup, projectionPlan, _schema);
            var summaryInstructions = SummaryInstruction.Create(request.Summaries, projectionPlan);
            var pageItems = new List<T>(request.Size);
            var totalCount = 0;
            var summaryAccumulator = GridSummaryEngine.CreateAccumulator(request.Summaries, _schema);
            var rowIndex = 0;
            var rowStore = new ProjectionValueStore(Math.Max(1, projectionPlan.ColumnCount));

            foreach (var row in source)
            {
                rowStore.Reset();
                var projectedRow = CreateProjectedRow(row, projectionPlan, rowIndex, rowStore);
                rowIndex++;
                if (!MatchesFilters(projectedRow, filterPlan))
                {
                    continue;
                }

                if (summaryInstructions.Length > 0)
                {
                    AddSummaryValues(projectedRow, summaryInstructions, summaryAccumulator);
                }

                if (totalCount >= request.Offset && pageItems.Count < request.Size)
                {
                    pageItems.Add(projectedRow.Item);
                }

                totalCount++;
            }

            return new GridQueryResult<T>(pageItems.ToArray(), totalCount, Array.Empty<GridGroupNode<T>>(), summaryAccumulator.ToSummarySet());
        }

        private GridQueryResult<T> ExecuteMaterialized(IEnumerable<T> source, GridQueryRequest request)
        {
            var filterProjectionPlan = ProjectionPlan.CreateFilterPlan(request.FilterGroup);
            var rowProjectionPlan = ProjectionPlan.CreateSortGroupPlan(request.Sorts, request.Groups);
            var filterPlan = FilterExecutionPlan.Create(request.FilterGroup, filterProjectionPlan, _schema);
            var sortInstructions = SortInstruction.Create(request.Sorts, rowProjectionPlan);
            var groupInstructions = GroupInstruction.Create(request.Groups, rowProjectionPlan);
            var summaryInstructions = SummaryInstruction.Create(request.Summaries, rowProjectionPlan);
            var materialized = MaterializeFilteredRows(source, filterProjectionPlan, rowProjectionPlan, filterPlan, sortInstructions, summaryInstructions);
            var totalCount = materialized.Rows.Length;
            var paged = materialized.Rows.Skip(request.Offset).Take(request.Size).Select(row => row.Item).ToArray();
            var grouped = BuildGroups(materialized.Rows, groupInstructions, 0, null, null);

            return new GridQueryResult<T>(paged, totalCount, grouped, materialized.Summary);
        }

        public IReadOnlyList<GridGroupNode<T>> BuildGroupedView(IReadOnlyList<T> source, IReadOnlyList<GridGroupDescriptor> groups, GridGroupExpansionState expansionState)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            var projectionPlan = ProjectionPlan.CreateSortGroupPlan(Array.Empty<GridSortDescriptor>(), groups);
            var groupInstructions = GroupInstruction.Create(groups, projectionPlan);
            var projectionStore = new ProjectionValueStore(Math.Max(1, source.Count * Math.Max(1, projectionPlan.ColumnCount)));
            var projectedRows = new ProjectedRow[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                projectedRows[i] = CreateProjectedRow(source[i], projectionPlan, i, projectionStore);
            }

            return BuildGroups(projectedRows, groupInstructions, 0, null, expansionState);
        }

        private MaterializedRows MaterializeFilteredRows(
            IEnumerable<T> source,
            ProjectionPlan filterProjectionPlan,
            ProjectionPlan rowProjectionPlan,
            FilterExecutionPlan filterPlan,
            IReadOnlyList<SortInstruction> sorts,
            IReadOnlyList<SummaryInstruction> summaries)
        {
            var estimatedRowCount = GetSourceCount(source);
            var filteredRows = estimatedRowCount > 0
                ? new List<ProjectedRow>(estimatedRowCount)
                : new List<ProjectedRow>();
            var filterRowStore = new ProjectionValueStore(Math.Max(1, filterProjectionPlan.ColumnCount));
            var projectionStore = new ProjectionValueStore(Math.Max(4, estimatedRowCount * Math.Max(1, rowProjectionPlan.ColumnCount)));
            var summaryAccumulator = GridSummaryEngine.CreateAccumulator(
                summaries.Select(summary => summary.Descriptor).ToArray(),
                _schema);
            var rowIndex = 0;

            foreach (var row in source)
            {
                filterRowStore.Reset();
                var filterRow = CreateProjectedRow(row, filterProjectionPlan, rowIndex, filterRowStore);
                rowIndex++;
                if (!MatchesFilters(filterRow, filterPlan))
                {
                    continue;
                }

                var projectedRow = CreateProjectedRow(row, rowProjectionPlan, rowIndex - 1, projectionStore);
                filteredRows.Add(projectedRow);
                if (summaries.Count == 0)
                {
                    continue;
                }

                AddSummaryValues(projectedRow, summaries, summaryAccumulator);
            }

            var sortedRows = filteredRows.ToArray();
            ApplySorting(sortedRows, sorts);
            return new MaterializedRows(sortedRows, summaryAccumulator.ToSummarySet());
        }

        private void ApplySorting(ProjectedRow[] source, IReadOnlyList<SortInstruction> sorts)
        {
            if (sorts.Count == 0 || source.Length <= 1)
            {
                return;
            }

            Array.Sort(source, (left, right) => CompareProjectedRows(left, right, sorts));
        }

        private int CompareProjectedRows(ProjectedRow left, ProjectedRow right, IReadOnlyList<SortInstruction> sorts)
        {
            for (var i = 0; i < sorts.Count; i++)
            {
                var sort = sorts[i];
                var result = Compare(left.GetValue(sort.Slot), right.GetValue(sort.Slot));
                if (result == 0)
                {
                    continue;
                }

                return sort.Direction == GridSortDirection.Descending ? -result : result;
            }

            return left.OriginalIndex.CompareTo(right.OriginalIndex);
        }

        private static bool MatchesFilters(ProjectedRow row, FilterExecutionPlan filterPlan)
        {
            if (filterPlan.Filters.Length == 0)
            {
                return true;
            }

            if (filterPlan.LogicalOperator == GridLogicalOperator.And)
            {
                return filterPlan.Filters.All(filter => MatchFilter(row, filter));
            }

            return filterPlan.Filters.Any(filter => MatchFilter(row, filter));
        }

        private static bool MatchFilter(ProjectedRow row, FilterInstruction filter)
        {
            var value = row.GetValue(filter.Slot);

            switch (filter.Operator)
            {
                case GridFilterOperator.Equals:
                    return GridValueComparer.Instance.Equals(value, filter.Value);
                case GridFilterOperator.Contains:
                    return (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                        .IndexOf(Convert.ToString(filter.Value, CultureInfo.InvariantCulture) ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
                case GridFilterOperator.StartsWith:
                    return (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
                        .StartsWith(Convert.ToString(filter.Value, CultureInfo.InvariantCulture) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                case GridFilterOperator.GreaterThan:
                    return Compare(value, filter.Value) > 0;
                case GridFilterOperator.LessThan:
                    return Compare(value, filter.Value) < 0;
                case GridFilterOperator.Between:
                    return Compare(value, filter.Value) >= 0 && Compare(value, filter.SecondValue) <= 0;
                case GridFilterOperator.IsTrue:
                    return GridValueComparer.TryConvertToBoolean(value, out var boolValueTrue) && boolValueTrue;
                case GridFilterOperator.IsFalse:
                    return GridValueComparer.TryConvertToBoolean(value, out var boolValueFalse) && !boolValueFalse;
                case GridFilterOperator.Custom:
                    return filter.CustomPredicate != null && filter.CustomPredicate(value);
                default:
                    return true;
            }
        }

        private IReadOnlyList<GridGroupNode<T>> BuildGroups(IReadOnlyList<ProjectedRow> items, IReadOnlyList<GroupInstruction> groups, int level, string parentId, GridGroupExpansionState expansionState)
        {
            if (groups.Count == 0 || level >= groups.Count)
            {
                return Array.Empty<GridGroupNode<T>>();
            }

            var descriptor = groups[level];
            var grouped = items
                .GroupBy(row => row.GetValue(descriptor.Slot), GridValueComparer.Instance)
                .OrderBy(group => group.Key, GridValueComparer.Instance)
                .ToList();

            if (descriptor.Direction == GridSortDirection.Descending)
            {
                grouped.Reverse();
            }

            var nodes = new List<GridGroupNode<T>>(grouped.Count);
            foreach (var group in grouped)
            {
                var groupedRows = group.ToArray();
                var groupedItems = groupedRows.Select(row => row.Item).ToArray();
                var groupId = GridGroupNode<T>.BuildStableId(parentId, descriptor.ColumnId, group.Key);
                var children = BuildGroups(groupedRows, groups, level + 1, groupId, expansionState);
                var node = new GridGroupNode<T>(groupId, descriptor.ColumnId, group.Key, level, groupedItems, children, groupedItems.Length)
                {
                    IsExpanded = expansionState == null || expansionState.IsExpanded(groupId),
                };
                nodes.Add(node);
            }

            return nodes;
        }

        private IReadOnlyList<GroupRangeNode> BuildGroupRanges(
            IReadOnlyList<ProjectedRow> items,
            IReadOnlyList<GroupInstruction> groups,
            int level,
            int startIndex,
            int endExclusive,
            string parentId,
            GridGroupExpansionState expansionState,
            IList<string> groupIds)
        {
            if (groups.Count == 0 || level >= groups.Count || startIndex >= endExclusive)
            {
                return Array.Empty<GroupRangeNode>();
            }

            var nodes = new List<GroupRangeNode>();
            var descriptor = groups[level];
            var currentStart = startIndex;
            while (currentStart < endExclusive)
            {
                var currentKey = items[currentStart].GetValue(descriptor.Slot);
                var currentEnd = ResolveGroupRunEnd(items, descriptor.Slot, currentKey, currentStart + 1, endExclusive);
                var groupId = GridGroupNode<T>.BuildStableId(parentId, descriptor.ColumnId, currentKey);
                groupIds.Add(groupId);

                var children = BuildGroupRanges(items, groups, level + 1, currentStart, currentEnd, groupId, expansionState, groupIds);
                var isExpanded = expansionState == null || expansionState.IsExpanded(groupId);
                nodes.Add(new GroupRangeNode(groupId, descriptor.ColumnId, currentKey, level, currentStart, currentEnd, isExpanded, children));
                currentStart = currentEnd;
            }

            return nodes;
        }

        private static int ResolveGroupRunEnd(IReadOnlyList<ProjectedRow> items, int slot, object key, int startIndex, int endExclusive)
        {
            var index = startIndex;
            while (index < endExclusive)
            {
                var value = items[index].GetValue(slot);
                if (!GridValueComparer.Instance.Equals(value, key))
                {
                    break;
                }

                index++;
            }

            return index;
        }

        private IReadOnlyList<GridGroupFlatRow<T>> BuildGroupedWindow(IReadOnlyList<GroupRangeNode> groups, IReadOnlyList<ProjectedRow> rows, int startIndex, int length)
        {
            if (groups.Count == 0 || length == 0)
            {
                return Array.Empty<GridGroupFlatRow<T>>();
            }

            var endExclusive = startIndex + length;
            var cursor = 0;
            var result = new List<GridGroupFlatRow<T>>(length);

            foreach (var group in groups)
            {
                AppendGroupWindow(group, rows, startIndex, endExclusive, result, ref cursor);
                if (result.Count >= length)
                {
                    break;
                }
            }

            return result;
        }

        private void AppendGroupWindow(GroupRangeNode group, IReadOnlyList<ProjectedRow> rows, int startIndex, int endExclusive, IList<GridGroupFlatRow<T>> result, ref int cursor)
        {
            if (cursor >= endExclusive)
            {
                return;
            }

            if (cursor + group.VisibleRowCount <= startIndex)
            {
                cursor += group.VisibleRowCount;
                return;
            }

            if (cursor >= startIndex)
            {
                result.Add(GridGroupFlatRow<T>.CreateGroupHeader(group.Id, group.ColumnId, group.Key, group.ItemCount, group.Level, group.IsExpanded));
            }

            cursor++;
            if (!group.IsExpanded || cursor >= endExclusive)
            {
                return;
            }

            if (group.Children.Count > 0)
            {
                foreach (var child in group.Children)
                {
                    AppendGroupWindow(child, rows, startIndex, endExclusive, result, ref cursor);
                    if (cursor >= endExclusive)
                    {
                        return;
                    }
                }

                return;
            }

            for (var rowIndex = group.StartIndex; rowIndex < group.EndExclusive; rowIndex++)
            {
                if (cursor >= startIndex && cursor < endExclusive)
                {
                    result.Add(GridGroupFlatRow<T>.CreateDataRow(rows[rowIndex].Item, group.Level + 1));
                }

                cursor++;
                if (cursor >= endExclusive)
                {
                    return;
                }
            }
        }

        private static IReadOnlyList<GridSortDescriptor> BuildEffectiveGroupSorts(IReadOnlyList<GridSortDescriptor> sorts, IReadOnlyList<GridGroupDescriptor> groups)
        {
            var effectiveSorts = new List<GridSortDescriptor>(groups.Count + sorts.Count);
            foreach (var group in groups)
            {
                effectiveSorts.Add(new GridSortDescriptor(group.ColumnId, group.Direction));
            }

            foreach (var sort in sorts)
            {
                if (groups.Any(group => string.Equals(group.ColumnId, sort.ColumnId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                effectiveSorts.Add(sort);
            }

            return effectiveSorts;
        }

        private ProjectedRow CreateProjectedRow(T row, ProjectionPlan projectionPlan, int originalIndex, ProjectionValueStore valueStore)
        {
            if (projectionPlan.ColumnCount == 0)
            {
                return new ProjectedRow(row, valueStore, 0, 0, originalIndex);
            }

            var valueOffset = valueStore.Reserve(projectionPlan.ColumnCount);
            for (var i = 0; i < projectionPlan.ColumnCount; i++)
            {
                var columnId = projectionPlan.ColumnIds[i];
                valueStore.Set(valueOffset + i, NormalizeValue(columnId, _accessor.GetValue(row, columnId)));
            }

            return new ProjectedRow(row, valueStore, valueOffset, projectionPlan.ColumnCount, originalIndex);
        }

        private object NormalizeValue(string columnId, object value)
        {
            return _schema == null ? value : _schema.NormalizeValue(columnId, value);
        }

        private static int Compare(object left, object right)
        {
            return GridValueComparer.Instance.Compare(left, right);
        }

        private void AddSummaryValues(ProjectedRow projectedRow, IReadOnlyList<SummaryInstruction> summaries, GridSummaryAccumulator accumulator)
        {
            for (var i = 0; i < summaries.Count; i++)
            {
                var summary = summaries[i];
                var value = summary.Slot >= 0
                    ? projectedRow.GetValue(summary.Slot)
                    : NormalizeValue(summary.ColumnId, _accessor.GetValue(projectedRow.Item, summary.ColumnId));
                accumulator.AddValue(summary.ColumnId, value);
            }
        }

        private static int GetSourceCount(IEnumerable<T> source)
        {
            if (source is IReadOnlyCollection<T> readOnlyCollection)
            {
                return readOnlyCollection.Count;
            }

            if (source is ICollection<T> collection)
            {
                return collection.Count;
            }

            if (source is System.Collections.ICollection nonGenericCollection)
            {
                return nonGenericCollection.Count;
            }

            return 0;
        }

        private sealed class MaterializedRows
        {
            public MaterializedRows(ProjectedRow[] rows, GridSummarySet summary)
            {
                Rows = rows ?? Array.Empty<ProjectedRow>();
                Summary = summary ?? GridSummarySet.Empty;
            }

            public ProjectedRow[] Rows { get; }

            public GridSummarySet Summary { get; }
        }

        private sealed class ProjectionValueStore
        {
            private object[] _buffer;
            private int _count;

            public ProjectionValueStore(int capacity)
            {
                _buffer = capacity <= 0 ? Array.Empty<object>() : new object[capacity];
            }

            public int Reserve(int length)
            {
                if (length <= 0)
                {
                    return 0;
                }

                EnsureCapacity(_count + length);
                var offset = _count;
                _count += length;
                return offset;
            }

            public void Set(int index, object value)
            {
                _buffer[index] = value;
            }

            public object Get(int index)
            {
                return index >= 0 && index < _count ? _buffer[index] : null;
            }

            public void Reset()
            {
                _count = 0;
            }

            private void EnsureCapacity(int requiredCapacity)
            {
                if (requiredCapacity <= _buffer.Length)
                {
                    return;
                }

                var newCapacity = _buffer.Length == 0 ? 4 : _buffer.Length;
                while (newCapacity < requiredCapacity)
                {
                    newCapacity *= 2;
                }

                Array.Resize(ref _buffer, newCapacity);
            }
        }

        private readonly struct ProjectedRow
        {
            public ProjectedRow(T item, ProjectionValueStore valueStore, int valueOffset, int valueCount, int originalIndex)
            {
                Item = item;
                ValueStore = valueStore;
                ValueOffset = valueOffset;
                ValueCount = valueCount;
                OriginalIndex = originalIndex;
            }

            public T Item { get; }

            public ProjectionValueStore ValueStore { get; }

            public int ValueOffset { get; }

            public int ValueCount { get; }

            public int OriginalIndex { get; }

            public object GetValue(int slot)
            {
                return slot >= 0 && slot < ValueCount && ValueStore != null
                    ? ValueStore.Get(ValueOffset + slot)
                    : null;
            }
        }

        private sealed class ProjectionPlan
        {
            private readonly Dictionary<string, int> _slotByColumnId;

            private ProjectionPlan(string[] columnIds)
            {
                ColumnIds = columnIds ?? Array.Empty<string>();
                _slotByColumnId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < ColumnIds.Length; i++)
                {
                    _slotByColumnId[ColumnIds[i]] = i;
                }
            }

            public string[] ColumnIds { get; }

            public int ColumnCount => ColumnIds.Length;

            public int GetSlot(string columnId)
            {
                int slot;
                return _slotByColumnId.TryGetValue(columnId ?? string.Empty, out slot) ? slot : -1;
            }

            public static ProjectionPlan CreateFilterPlan(GridFilterGroup filterGroup)
            {
                var columns = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                AddColumns(columns, seen, filterGroup == null ? Array.Empty<string>() : filterGroup.Filters.Select(filter => filter.ColumnId));
                return new ProjectionPlan(columns.ToArray());
            }

            public static ProjectionPlan CreateSortGroupPlan(
                IReadOnlyList<GridSortDescriptor> sorts,
                IReadOnlyList<GridGroupDescriptor> groups)
            {
                var columns = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                AddColumns(columns, seen, sorts == null ? Array.Empty<string>() : sorts.Select(sort => sort.ColumnId));
                AddColumns(columns, seen, groups == null ? Array.Empty<string>() : groups.Select(group => group.ColumnId));

                return new ProjectionPlan(columns.ToArray());
            }

            private static void AddColumns(ICollection<string> columns, ISet<string> seen, IEnumerable<string> values)
            {
                foreach (var value in values)
                {
                    if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                    {
                        continue;
                    }

                    columns.Add(value);
                }
            }
        }

        private sealed class FilterExecutionPlan
        {
            public static readonly FilterExecutionPlan Empty = new FilterExecutionPlan(Array.Empty<FilterInstruction>(), GridLogicalOperator.And);

            private FilterExecutionPlan(FilterInstruction[] filters, GridLogicalOperator logicalOperator)
            {
                Filters = filters ?? Array.Empty<FilterInstruction>();
                LogicalOperator = logicalOperator;
            }

            public FilterInstruction[] Filters { get; }

            public GridLogicalOperator LogicalOperator { get; }

            public static FilterExecutionPlan Create(GridFilterGroup filterGroup, ProjectionPlan projectionPlan, GridQuerySchema schema)
            {
                if (filterGroup == null || filterGroup.Filters.Count == 0)
                {
                    return Empty;
                }

                var filters = filterGroup.Filters
                    .Select(filter => new FilterInstruction(
                        projectionPlan.GetSlot(filter.ColumnId),
                        filter.Operator,
                        NormalizeValue(schema, filter.ColumnId, filter.Value),
                        NormalizeValue(schema, filter.ColumnId, filter.SecondValue),
                        filter.CustomPredicate))
                    .ToArray();
                return new FilterExecutionPlan(filters, filterGroup.LogicalOperator);
            }

            private static object NormalizeValue(GridQuerySchema schema, string columnId, object value)
            {
                if (value == null)
                {
                    return null;
                }

                return schema == null ? value : schema.NormalizeValue(columnId, value);
            }
        }

        private sealed class FilterInstruction
        {
            public FilterInstruction(int slot, GridFilterOperator @operator, object value, object secondValue, Func<object, bool> customPredicate)
            {
                Slot = slot;
                Operator = @operator;
                Value = value;
                SecondValue = secondValue;
                CustomPredicate = customPredicate;
            }

            public int Slot { get; }

            public GridFilterOperator Operator { get; }

            public object Value { get; }

            public object SecondValue { get; }

            public Func<object, bool> CustomPredicate { get; }
        }

        private sealed class SortInstruction
        {
            private SortInstruction(int slot, GridSortDirection direction)
            {
                Slot = slot;
                Direction = direction;
            }

            public int Slot { get; }

            public GridSortDirection Direction { get; }

            public static SortInstruction[] Create(IReadOnlyList<GridSortDescriptor> sorts, ProjectionPlan projectionPlan)
            {
                if (sorts == null || sorts.Count == 0)
                {
                    return Array.Empty<SortInstruction>();
                }

                return sorts
                    .Select(sort => new SortInstruction(projectionPlan.GetSlot(sort.ColumnId), sort.Direction))
                    .ToArray();
            }
        }

        private sealed class SummaryInstruction
        {
            private SummaryInstruction(GridSummaryDescriptor descriptor, int slot)
            {
                Descriptor = descriptor;
                ColumnId = descriptor.ColumnId;
                Slot = slot;
            }

            public GridSummaryDescriptor Descriptor { get; }

            public string ColumnId { get; }

            public int Slot { get; }

            public static SummaryInstruction[] Create(IReadOnlyList<GridSummaryDescriptor> summaries, ProjectionPlan projectionPlan)
            {
                if (summaries == null || summaries.Count == 0)
                {
                    return Array.Empty<SummaryInstruction>();
                }

                return summaries
                    .Select(summary => new SummaryInstruction(summary, projectionPlan.GetSlot(summary.ColumnId)))
                    .ToArray();
            }
        }

        private sealed class GroupInstruction
        {
            private GroupInstruction(string columnId, int slot, GridSortDirection direction)
            {
                ColumnId = columnId;
                Slot = slot;
                Direction = direction;
            }

            public string ColumnId { get; }

            public int Slot { get; }

            public GridSortDirection Direction { get; }

            public static GroupInstruction[] Create(IReadOnlyList<GridGroupDescriptor> groups, ProjectionPlan projectionPlan)
            {
                if (groups == null || groups.Count == 0)
                {
                    return Array.Empty<GroupInstruction>();
                }

                return groups
                    .Select(group => new GroupInstruction(group.ColumnId, projectionPlan.GetSlot(group.ColumnId), group.Direction))
                    .ToArray();
            }
        }

        private sealed class GroupRangeNode
        {
            public GroupRangeNode(
                string id,
                string columnId,
                object key,
                int level,
                int startIndex,
                int endExclusive,
                bool isExpanded,
                IReadOnlyList<GroupRangeNode> children)
            {
                Id = id;
                ColumnId = columnId;
                Key = key;
                Level = level;
                StartIndex = startIndex;
                EndExclusive = endExclusive;
                IsExpanded = isExpanded;
                Children = children ?? Array.Empty<GroupRangeNode>();
                ItemCount = endExclusive - startIndex;
                VisibleRowCount = 1 + (isExpanded
                    ? (Children.Count > 0 ? Children.Sum(child => child.VisibleRowCount) : ItemCount)
                    : 0);
            }

            public string Id { get; }

            public string ColumnId { get; }

            public object Key { get; }

            public int Level { get; }

            public int StartIndex { get; }

            public int EndExclusive { get; }

            public int ItemCount { get; }

            public bool IsExpanded { get; }

            public int VisibleRowCount { get; }

            public IReadOnlyList<GroupRangeNode> Children { get; }
        }
    }
}
