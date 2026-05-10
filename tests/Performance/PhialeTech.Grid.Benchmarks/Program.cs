using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using PhialeGrid.Core;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Editing;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Core.Surface;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeGrid.Benchmarks
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var rows = DemoGisDataLoader.LoadDefaultRecords();

            Console.WriteLine("PhialeGrid Core query benchmark");
            Console.WriteLine($"Dataset rows: {rows.Count}");
            Console.WriteLine($"Benchmark date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();

            if (args.Any(arg => string.Equals(arg, "navigation-only", StringComparison.OrdinalIgnoreCase)))
            {
                RunNavigationBenchmarks(rows);
                return 0;
            }

            var scenarios = CreateScenarios();

            foreach (var scenario in scenarios)
            {
                var expansion = scenario.BuildExpansionState(rows, CreateEngine(CreateAccessor()));
                var legacyAccessor = CreateAccessor();
                var optimizedAccessor = CreateAccessor();
                var optimizedEngine = CreateEngine(optimizedAccessor);

                var legacy = Measure(() => RunLegacy(rows, scenario, expansion, legacyAccessor), 40, 500);
                var optimized = Measure(() => RunOptimized(optimizedEngine, rows, scenario, expansion, optimizedAccessor), 40, 500);
                ValidateEquivalent(scenario.Name, legacy.Sample, optimized.Sample);

                Console.WriteLine($"Scenario: {scenario.Name}");
                Console.WriteLine($"  Legacy mean:      {legacy.MeanMilliseconds:F4} ms/op");
                Console.WriteLine($"  Legacy alloc:     {legacy.MeanAllocatedBytes:F0} B/op");
                Console.WriteLine($"  Legacy accessor:  {legacy.Sample.AccessorCalls} calls");
                Console.WriteLine($"  Optimized mean:   {optimized.MeanMilliseconds:F4} ms/op");
                Console.WriteLine($"  Optimized alloc:  {optimized.MeanAllocatedBytes:F0} B/op");
                Console.WriteLine($"  Optimized accessor:{optimized.Sample.AccessorCalls} calls");
                Console.WriteLine($"  Speedup:          {(legacy.MeanMilliseconds / optimized.MeanMilliseconds):F2}x");
                Console.WriteLine($"  Alloc ratio:      {(optimized.MeanAllocatedBytes / legacy.MeanAllocatedBytes):P1}");
                Console.WriteLine($"  Accessor ratio:   {(optimized.Sample.AccessorCalls / (double)legacy.Sample.AccessorCalls):P1}");
                Console.WriteLine($"  Visible rows:     {optimized.Sample.VisibleRowCount}");
                Console.WriteLine($"  Total items:      {optimized.Sample.TotalItemCount}");
                Console.WriteLine();
            }

            RunNavigationBenchmarks(rows);

            return 0;
        }

        private static BenchmarkMeasurement Measure(Func<BenchmarkSnapshot> operation, int warmupIterations, int measuredIterations)
        {
            for (var i = 0; i < warmupIterations; i++)
            {
                operation();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            BenchmarkSnapshot sample = null;
            for (var i = 0; i < measuredIterations; i++)
            {
                var current = operation();
                if (sample == null)
                {
                    sample = current;
                }
            }

            stopwatch.Stop();
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            return new BenchmarkMeasurement(
                sample,
                stopwatch.Elapsed.TotalMilliseconds / measuredIterations,
                (allocatedAfter - allocatedBefore) / (double)measuredIterations);
        }

        private static BenchmarkSnapshot RunLegacy(
            IReadOnlyList<DemoGisRecordViewModel> rows,
            BenchmarkScenario scenario,
            GridGroupExpansionState expansion,
            CountingGridRowAccessor<DemoGisRecordViewModel> accessor)
        {
            accessor.Reset();

            var effectiveSorts = BuildEffectiveSorts(scenario.Sorts, scenario.Groups);
            var filteredRows = rows
                .Select((item, index) => new LegacyIndexedRow<DemoGisRecordViewModel>(item, index))
                .Where(row => MatchesFilters(row.Item, scenario.FilterGroup, accessor))
                .ToArray();
            var summary = GridSummaryEngine.Calculate(filteredRows.Select(row => row.Item).ToArray(), scenario.Summaries, accessor);
            Array.Sort(filteredRows, (left, right) => CompareRows(left, right, effectiveSorts, accessor));

            if (scenario.Groups.Count == 0)
            {
                var page = filteredRows
                    .Skip(scenario.Offset)
                    .Take(scenario.Size)
                    .Select(row => GridGroupFlatRow<DemoGisRecordViewModel>.CreateDataRow(row.Item, 0))
                    .ToArray();
                return new BenchmarkSnapshot(
                    filteredRows.Length,
                    filteredRows.Length,
                    0,
                    Array.Empty<string>(),
                    summary,
                    ToShape(page),
                    accessor.TotalCalls);
            }

            var groupIds = new List<string>();
            var groups = BuildLegacyGroups(filteredRows, scenario.Groups, 0, filteredRows.Length, 0, null, accessor, expansion, groupIds);
            var window = GridGroupFlatRowWindowBuilder.BuildWindow(groups, scenario.Offset, scenario.Size);
            return new BenchmarkSnapshot(
                window.TotalRowCount,
                filteredRows.Length,
                groups.Count,
                groupIds.ToArray(),
                summary,
                ToShape(window.Rows),
                accessor.TotalCalls);
        }

        private static BenchmarkSnapshot RunOptimized(
            GridQueryEngine<DemoGisRecordViewModel> engine,
            IReadOnlyList<DemoGisRecordViewModel> rows,
            BenchmarkScenario scenario,
            GridGroupExpansionState expansion,
            CountingGridRowAccessor<DemoGisRecordViewModel> accessor)
        {
            accessor.Reset();

            var result = engine.ExecuteGroupedWindow(rows, new GridGroupedQueryRequest(
                scenario.Offset,
                scenario.Size,
                scenario.Sorts,
                scenario.FilterGroup,
                scenario.Groups,
                scenario.Summaries,
                expansion));
            return new BenchmarkSnapshot(
                result.VisibleRowCount,
                result.TotalItemCount,
                result.TopLevelGroupCount,
                result.GroupIds,
                result.Summary,
                ToShape(result.Rows),
                accessor.TotalCalls);
        }

        private static IReadOnlyList<BenchmarkScenario> CreateScenarios()
        {
            return new[]
            {
                new BenchmarkScenario(
                    "Category collapsed",
                    0,
                    48,
                    Array.Empty<GridSortDescriptor>(),
                    GridFilterGroup.EmptyAnd(),
                    new[] { new GridGroupDescriptor("Category") },
                    new[]
                    {
                        new GridSummaryDescriptor("AreaSquareMeters", GridSummaryType.Sum),
                        new GridSummaryDescriptor("LengthMeters", GridSummaryType.Sum),
                        new GridSummaryDescriptor("ObjectId", GridSummaryType.Count),
                    },
                    collapseAllAfterDiscovery: true),
                new BenchmarkScenario(
                    "District + Status expanded, filtered",
                    24,
                    64,
                    new[] { new GridSortDescriptor("LastInspection", GridSortDirection.Descending) },
                    new GridFilterGroup(new[]
                    {
                        new GridFilterDescriptor("Municipality", GridFilterOperator.Contains, "Wro"),
                    }, GridLogicalOperator.And),
                    new[]
                    {
                        new GridGroupDescriptor("District"),
                        new GridGroupDescriptor("Status"),
                    },
                    new[]
                    {
                        new GridSummaryDescriptor("AreaSquareMeters", GridSummaryType.Sum),
                        new GridSummaryDescriptor("LengthMeters", GridSummaryType.Sum),
                    },
                    collapseAllAfterDiscovery: false),
            };
        }

        private static void RunNavigationBenchmarks(IReadOnlyList<DemoGisRecordViewModel> rows)
        {
            const int warmupIterations = 12;
            const int measuredIterations = 80;
            const int transitionsPerIteration = 256;

            var navigationBenchmarks = new INavigationBenchmark[]
            {
                new LocalRowStateNavigationBenchmark(rows),
                new SurfaceCoordinatorNavigationBenchmark(rows),
                new EditSessionCurrentRecordBenchmark(rows),
                new EditSessionContextWithRebuildBenchmark(rows),
            };

            Console.WriteLine("Navigation benchmark");
            Console.WriteLine($"Navigation rows: {rows.Count}");
            Console.WriteLine($"Transitions per iteration: {transitionsPerIteration}");

            var measurements = new List<(string Name, NavigationMeasurement Measurement)>();
            foreach (var benchmark in navigationBenchmarks)
            {
                using (benchmark)
                {
                    var measurement = MeasureNavigation(benchmark, warmupIterations, measuredIterations, transitionsPerIteration);
                    measurements.Add((benchmark.Name, measurement));
                }
            }

            var baseline = measurements[0].Measurement;
            foreach (var item in measurements)
            {
                var perTransitionMicroseconds = item.Measurement.MeanMilliseconds * 1000d / item.Measurement.TransitionsPerIteration;
                Console.WriteLine($"Scenario: {item.Name}");
                Console.WriteLine($"  Mean:             {item.Measurement.MeanMilliseconds:F4} ms/iteration");
                Console.WriteLine($"  Mean per move:    {perTransitionMicroseconds:F2} us/transition");
                Console.WriteLine($"  Alloc:            {item.Measurement.MeanAllocatedBytes:F0} B/iteration");
                Console.WriteLine($"  Last record:      {item.Measurement.Sample.CurrentRecordId}");
                Console.WriteLine($"  Edited hits:      {item.Measurement.Sample.EditedHits}");
                Console.WriteLine($"  Invalid hits:     {item.Measurement.Sample.InvalidHits}");
                Console.WriteLine($"  Notifications:    {item.Measurement.Sample.NotificationCount}");
                Console.WriteLine($"  Projection rows:  {item.Measurement.Sample.ProjectedRecordCount}");
                if (!ReferenceEquals(item.Measurement, baseline))
                {
                    Console.WriteLine($"  Slowdown vs local:{(item.Measurement.MeanMilliseconds / baseline.MeanMilliseconds):F2}x");
                }

                Console.WriteLine();
            }
        }

        private static NavigationMeasurement MeasureNavigation(
            INavigationBenchmark benchmark,
            int warmupIterations,
            int measuredIterations,
            int transitionsPerIteration)
        {
            benchmark.Reset();
            for (var i = 0; i < warmupIterations; i++)
            {
                benchmark.RunIteration(transitionsPerIteration);
            }

            benchmark.Reset();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            NavigationSnapshot sample = null;
            for (var i = 0; i < measuredIterations; i++)
            {
                var current = benchmark.RunIteration(transitionsPerIteration);
                if (sample == null)
                {
                    sample = current;
                }
            }

            stopwatch.Stop();
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            return new NavigationMeasurement(
                sample,
                stopwatch.Elapsed.TotalMilliseconds / measuredIterations,
                (allocatedAfter - allocatedBefore) / (double)measuredIterations,
                transitionsPerIteration);
        }

        private static IReadOnlyList<GridSortDescriptor> BuildEffectiveSorts(IReadOnlyList<GridSortDescriptor> sorts, IReadOnlyList<GridGroupDescriptor> groups)
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

        private static CountingGridRowAccessor<DemoGisRecordViewModel> CreateAccessor()
        {
            return new CountingGridRowAccessor<DemoGisRecordViewModel>((row, columnId) =>
            {
                switch (columnId)
                {
                    case "Id":
                    case "ObjectId":
                        return row.ObjectId;
                    case "Category":
                        return row.Category;
                    case "ObjectName":
                        return row.ObjectName;
                    case "GeometryType":
                        return row.GeometryType;
                    case "Municipality":
                        return row.Municipality;
                    case "District":
                        return row.District;
                    case "Status":
                        return row.Status;
                    case "Priority":
                        return row.Priority;
                    case "AreaSquareMeters":
                        return row.AreaSquareMeters;
                    case "LengthMeters":
                        return row.LengthMeters;
                    case "LastInspection":
                        return row.LastInspection;
                    case "Owner":
                        return row.Owner;
                    default:
                        return null;
                }
            });
        }

        private static GridQueryEngine<DemoGisRecordViewModel> CreateEngine(IGridRowAccessor<DemoGisRecordViewModel> accessor)
        {
            return new GridQueryEngine<DemoGisRecordViewModel>(accessor);
        }

        private static void ValidateEquivalent(string scenarioName, BenchmarkSnapshot legacy, BenchmarkSnapshot optimized)
        {
            if (legacy.VisibleRowCount != optimized.VisibleRowCount
                || legacy.TotalItemCount != optimized.TotalItemCount
                || legacy.TopLevelGroupCount != optimized.TopLevelGroupCount
                || !legacy.GroupIds.SequenceEqual(optimized.GroupIds)
                || !legacy.RowShape.SequenceEqual(optimized.RowShape)
                || !SummaryEquals(legacy.Summary, optimized.Summary))
            {
                throw new InvalidOperationException($"Benchmark scenario '{scenarioName}' produced different legacy and optimized results.");
            }
        }

        private static bool SummaryEquals(GridSummarySet left, GridSummarySet right)
        {
            var leftValues = left.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
            var rightValues = right.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
            if (leftValues.Length != rightValues.Length)
            {
                return false;
            }

            for (var i = 0; i < leftValues.Length; i++)
            {
                if (!string.Equals(leftValues[i].Key, rightValues[i].Key, StringComparison.Ordinal))
                {
                    return false;
                }

                var leftText = Convert.ToString(leftValues[i].Value, CultureInfo.InvariantCulture) ?? string.Empty;
                var rightText = Convert.ToString(rightValues[i].Value, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!string.Equals(leftText, rightText, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesFilters<T>(T row, GridFilterGroup filterGroup, IGridRowAccessor<T> accessor)
        {
            if (filterGroup == null || filterGroup.Filters.Count == 0)
            {
                return true;
            }

            if (filterGroup.LogicalOperator == GridLogicalOperator.And)
            {
                return filterGroup.Filters.All(filter => MatchesFilter(row, filter, accessor));
            }

            return filterGroup.Filters.Any(filter => MatchesFilter(row, filter, accessor));
        }

        private static bool MatchesFilter<T>(T row, GridFilterDescriptor filter, IGridRowAccessor<T> accessor)
        {
            var value = accessor.GetValue(row, filter.ColumnId);
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
                    return GridValueComparer.Instance.Compare(value, filter.Value) > 0;
                case GridFilterOperator.LessThan:
                    return GridValueComparer.Instance.Compare(value, filter.Value) < 0;
                case GridFilterOperator.Between:
                    return GridValueComparer.Instance.Compare(value, filter.Value) >= 0
                        && GridValueComparer.Instance.Compare(value, filter.SecondValue) <= 0;
                case GridFilterOperator.IsTrue:
                    return GridValueComparer.TryConvertToBoolean(value, out var boolTrue) && boolTrue;
                case GridFilterOperator.IsFalse:
                    return GridValueComparer.TryConvertToBoolean(value, out var boolFalse) && !boolFalse;
                case GridFilterOperator.Custom:
                    return filter.CustomPredicate != null && filter.CustomPredicate(value);
                default:
                    return true;
            }
        }

        private static int CompareRows<T>(LegacyIndexedRow<T> left, LegacyIndexedRow<T> right, IReadOnlyList<GridSortDescriptor> sorts, IGridRowAccessor<T> accessor)
        {
            for (var i = 0; i < sorts.Count; i++)
            {
                var sort = sorts[i];
                var result = GridValueComparer.Instance.Compare(
                    accessor.GetValue(left.Item, sort.ColumnId),
                    accessor.GetValue(right.Item, sort.ColumnId));
                if (result == 0)
                {
                    continue;
                }

                return sort.Direction == GridSortDirection.Descending ? -result : result;
            }

            return left.OriginalIndex.CompareTo(right.OriginalIndex);
        }

        private static IReadOnlyList<GridGroupNode<T>> BuildLegacyGroups<T>(
            LegacyIndexedRow<T>[] rows,
            IReadOnlyList<GridGroupDescriptor> groups,
            int startIndex,
            int endExclusive,
            int level,
            string parentId,
            IGridRowAccessor<T> accessor,
            GridGroupExpansionState expansion,
            IList<string> groupIds)
        {
            if (groups.Count == 0 || level >= groups.Count || startIndex >= endExclusive)
            {
                return Array.Empty<GridGroupNode<T>>();
            }

            var descriptor = groups[level];
            var result = new List<GridGroupNode<T>>();
            var index = startIndex;
            while (index < endExclusive)
            {
                var key = accessor.GetValue(rows[index].Item, descriptor.ColumnId);
                var next = index + 1;
                while (next < endExclusive && GridValueComparer.Instance.Equals(accessor.GetValue(rows[next].Item, descriptor.ColumnId), key))
                {
                    next++;
                }

                var groupId = GridGroupNode<T>.BuildStableId(parentId, descriptor.ColumnId, key);
                groupIds.Add(groupId);
                var items = new T[next - index];
                for (var itemIndex = index; itemIndex < next; itemIndex++)
                {
                    items[itemIndex - index] = rows[itemIndex].Item;
                }

                var children = BuildLegacyGroups(rows, groups, index, next, level + 1, groupId, accessor, expansion, groupIds);
                var node = new GridGroupNode<T>(groupId, descriptor.ColumnId, key, level, items, children, items.Length)
                {
                    IsExpanded = expansion == null || expansion.IsExpanded(groupId),
                };
                result.Add(node);
                index = next;
            }

            return result;
        }

        private static string[] ToShape(IReadOnlyList<GridGroupFlatRow<DemoGisRecordViewModel>> rows)
        {
            return rows.Select(row => row.Kind == GridGroupFlatRowKind.GroupHeader
                ? $"H:{row.Level}:{row.GroupColumnId}:{row.GroupKey}:{row.GroupItemCount}:{row.IsExpanded}"
                : $"D:{row.Level}:{row.Item.ObjectId}").ToArray();
        }

        private sealed class BenchmarkScenario
        {
            public BenchmarkScenario(
                string name,
                int offset,
                int size,
                IReadOnlyList<GridSortDescriptor> sorts,
                GridFilterGroup filterGroup,
                IReadOnlyList<GridGroupDescriptor> groups,
                IReadOnlyList<GridSummaryDescriptor> summaries,
                bool collapseAllAfterDiscovery)
            {
                Name = name;
                Offset = offset;
                Size = size;
                Sorts = sorts;
                FilterGroup = filterGroup;
                Groups = groups;
                Summaries = summaries;
                CollapseAllAfterDiscovery = collapseAllAfterDiscovery;
            }

            public string Name { get; }

            public int Offset { get; }

            public int Size { get; }

            public IReadOnlyList<GridSortDescriptor> Sorts { get; }

            public GridFilterGroup FilterGroup { get; }

            public IReadOnlyList<GridGroupDescriptor> Groups { get; }

            public IReadOnlyList<GridSummaryDescriptor> Summaries { get; }

            public bool CollapseAllAfterDiscovery { get; }

            public GridGroupExpansionState BuildExpansionState(IReadOnlyList<DemoGisRecordViewModel> rows, GridQueryEngine<DemoGisRecordViewModel> engine)
            {
                var expansion = new GridGroupExpansionState();
                if (!CollapseAllAfterDiscovery)
                {
                    return expansion;
                }

                var groups = engine.BuildGroupedView(rows, Groups, expansion);
                foreach (var groupId in FlattenGroupIds(groups))
                {
                    expansion.SetExpanded(groupId, false);
                }

                return expansion;
            }
        }

        private sealed class BenchmarkSnapshot
        {
            public BenchmarkSnapshot(
                int visibleRowCount,
                int totalItemCount,
                int topLevelGroupCount,
                IReadOnlyList<string> groupIds,
                GridSummarySet summary,
                IReadOnlyList<string> rowShape,
                int accessorCalls)
            {
                VisibleRowCount = visibleRowCount;
                TotalItemCount = totalItemCount;
                TopLevelGroupCount = topLevelGroupCount;
                GroupIds = groupIds ?? Array.Empty<string>();
                Summary = summary ?? GridSummarySet.Empty;
                RowShape = rowShape ?? Array.Empty<string>();
                AccessorCalls = accessorCalls;
            }

            public int VisibleRowCount { get; }

            public int TotalItemCount { get; }

            public int TopLevelGroupCount { get; }

            public IReadOnlyList<string> GroupIds { get; }

            public GridSummarySet Summary { get; }

            public IReadOnlyList<string> RowShape { get; }

            public int AccessorCalls { get; }
        }

        private sealed class BenchmarkMeasurement
        {
            public BenchmarkMeasurement(BenchmarkSnapshot sample, double meanMilliseconds, double meanAllocatedBytes)
            {
                Sample = sample;
                MeanMilliseconds = meanMilliseconds;
                MeanAllocatedBytes = meanAllocatedBytes;
            }

            public BenchmarkSnapshot Sample { get; }

            public double MeanMilliseconds { get; }

            public double MeanAllocatedBytes { get; }
        }

        private sealed class CountingGridRowAccessor<T> : IGridRowAccessor<T>
        {
            private readonly Func<T, string, object> _resolver;

            public CountingGridRowAccessor(Func<T, string, object> resolver)
            {
                _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            }

            public int TotalCalls { get; private set; }

            public object GetValue(T row, string columnId)
            {
                TotalCalls++;
                return _resolver(row, columnId);
            }

            public void Reset()
            {
                TotalCalls = 0;
            }
        }

        private readonly struct LegacyIndexedRow<T>
        {
            public LegacyIndexedRow(T item, int originalIndex)
            {
                Item = item;
                OriginalIndex = originalIndex;
            }

            public T Item { get; }

            public int OriginalIndex { get; }
        }

        private static IReadOnlyList<string> FlattenGroupIds(IReadOnlyList<GridGroupNode<DemoGisRecordViewModel>> groups)
        {
            var ids = new List<string>();
            foreach (var group in groups)
            {
                ids.Add(group.Id);
                ids.AddRange(FlattenGroupIds(group.Children));
            }

            return ids;
        }

        private static IReadOnlyList<PhialeGrid.Core.Layout.GridColumnDefinition> CreateNavigationColumns()
        {
            return new[]
            {
                new PhialeGrid.Core.Layout.GridColumnDefinition { ColumnKey = "ObjectName", Header = "Object name", Width = 220, ValueType = typeof(string), ValueKind = "Text" },
                new PhialeGrid.Core.Layout.GridColumnDefinition { ColumnKey = "Status", Header = "Status", Width = 160, ValueType = typeof(string), ValueKind = "Text" },
                new PhialeGrid.Core.Layout.GridColumnDefinition { ColumnKey = "Owner", Header = "Owner", Width = 180, ValueType = typeof(string), ValueKind = "Text" },
            };
        }

        private static IReadOnlyList<GridRowDefinition> CreateNavigationRows(IReadOnlyList<DemoGisRecordViewModel> rows)
        {
            return rows.Select(row => new GridRowDefinition
            {
                RowKey = row.ObjectId,
                HeaderText = row.ObjectId,
                Height = 30,
                RepresentsDataRecord = true,
            }).ToArray();
        }

        private static HashSet<string> CreateEditedRowIds(IReadOnlyList<DemoGisRecordViewModel> rows)
        {
            return new HashSet<string>(
                rows.Where((_, index) => index % 9 == 0).Select(row => row.ObjectId),
                StringComparer.Ordinal);
        }

        private static HashSet<string> CreateInvalidRowIds(IReadOnlyList<DemoGisRecordViewModel> rows)
        {
            return new HashSet<string>(
                rows.Where((_, index) => index % 17 == 0).Select(row => row.ObjectId),
                StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, DemoGisRecordViewModel> CreateRowMap(IReadOnlyList<DemoGisRecordViewModel> rows)
        {
            return rows.ToDictionary(row => row.ObjectId, row => row, StringComparer.Ordinal);
        }

        private interface INavigationBenchmark : IDisposable
        {
            string Name { get; }

            void Reset();

            NavigationSnapshot RunIteration(int transitionsPerIteration);
        }

        private sealed class LocalRowStateNavigationBenchmark : INavigationBenchmark
        {
            private readonly string[] _rowIds;
            private readonly HashSet<string> _editedRowIds;
            private readonly HashSet<string> _invalidRowIds;
            private int _index;

            public LocalRowStateNavigationBenchmark(IReadOnlyList<DemoGisRecordViewModel> rows)
            {
                _rowIds = rows.Select(row => row.ObjectId).ToArray();
                _editedRowIds = CreateEditedRowIds(rows);
                _invalidRowIds = CreateInvalidRowIds(rows);
                _index = -1;
            }

            public string Name => "Local row-state structures";

            public void Reset()
            {
                _index = -1;
            }

            public NavigationSnapshot RunIteration(int transitionsPerIteration)
            {
                var currentRecordId = string.Empty;
                var editedHits = 0;
                var invalidHits = 0;

                for (var i = 0; i < transitionsPerIteration; i++)
                {
                    _index = (_index + 1) % _rowIds.Length;
                    currentRecordId = _rowIds[_index];
                    if (_editedRowIds.Contains(currentRecordId))
                    {
                        editedHits++;
                    }

                    if (_invalidRowIds.Contains(currentRecordId))
                    {
                        invalidHits++;
                    }
                }

                return new NavigationSnapshot(currentRecordId, editedHits, invalidHits, 0, _editedRowIds.Count + _invalidRowIds.Count);
            }

            public void Dispose()
            {
            }
        }

        private sealed class SurfaceCoordinatorNavigationBenchmark : INavigationBenchmark
        {
            private const string CurrentColumnId = "ObjectName";

            private readonly GridSurfaceCoordinator _coordinator;
            private readonly string[] _rowIds;
            private int _index;

            public SurfaceCoordinatorNavigationBenchmark(IReadOnlyList<DemoGisRecordViewModel> rows)
            {
                _rowIds = rows.Select(row => row.ObjectId).ToArray();
                _coordinator = new GridSurfaceCoordinator
                {
                    CellValueProvider = new DemoGridCellValueProvider(CreateRowMap(rows)),
                    ShowCurrentRecordIndicator = true,
                };
                _coordinator.Initialize(CreateNavigationColumns(), CreateNavigationRows(rows));
                _coordinator.SetViewportSize(1280, 720);
                _coordinator.SetEditedRows(CreateEditedRowIds(rows));
                _coordinator.SetInvalidRows(CreateInvalidRowIds(rows));
                _index = -1;
            }

            public string Name => "GridSurfaceCoordinator current-row move";

            public void Reset()
            {
                _index = -1;
            }

            public NavigationSnapshot RunIteration(int transitionsPerIteration)
            {
                var currentRecordId = string.Empty;
                var editedHits = 0;
                var invalidHits = 0;
                var projectedRows = 0;

                for (var i = 0; i < transitionsPerIteration; i++)
                {
                    _index = (_index + 1) % _rowIds.Length;
                    currentRecordId = _rowIds[_index];
                    _coordinator.SetCurrentCell(currentRecordId, CurrentColumnId);
                    var snapshot = _coordinator.GetCurrentSnapshot();
                    if (snapshot?.CurrentCell != null)
                    {
                        currentRecordId = snapshot.CurrentCell.RowKey;
                    }

                    var rowState = snapshot?.Rows.FirstOrDefault(row => string.Equals(row.RowKey, currentRecordId, StringComparison.Ordinal));
                    if (rowState != null)
                    {
                        if (rowState.HasPendingChanges)
                        {
                            editedHits++;
                        }

                        if (rowState.HasValidationError)
                        {
                            invalidHits++;
                        }
                    }

                    projectedRows = snapshot?.Rows.Count ?? 0;
                }

                return new NavigationSnapshot(currentRecordId, editedHits, invalidHits, 0, projectedRows);
            }

            public void Dispose()
            {
            }
        }

        private class EditSessionCurrentRecordBenchmark : INavigationBenchmark
        {
            protected readonly EditSessionContext<DemoGisRecordViewModel> Context;
            protected readonly string[] RowIds;
            private int _index;
            private int _notifications;

            public EditSessionCurrentRecordBenchmark(IReadOnlyList<DemoGisRecordViewModel> rows)
            {
                RowIds = rows.Select(row => row.ObjectId).ToArray();
                Context = new EditSessionContext<DemoGisRecordViewModel>(
                    new InMemoryEditSessionDataSource<DemoGisRecordViewModel>(rows),
                    row => row.ObjectId);
                Context.CurrentRecordChanged += HandleCurrentRecordChanged;
                _index = -1;
            }

            public virtual string Name => "EditSessionContext current-record only";

            public void Reset()
            {
                _index = -1;
                _notifications = 0;
                Context.ClearCurrentRecord();
            }

            public virtual NavigationSnapshot RunIteration(int transitionsPerIteration)
            {
                var notificationsBefore = _notifications;
                var currentRecordId = string.Empty;

                for (var i = 0; i < transitionsPerIteration; i++)
                {
                    _index = (_index + 1) % RowIds.Length;
                    Context.SetCurrentRecord(RowIds[_index]);
                    currentRecordId = Context.CurrentRecordId;
                }

                return new NavigationSnapshot(
                    currentRecordId,
                    0,
                    0,
                    _notifications - notificationsBefore,
                    Context.SurfaceStateProjection.RecordStates.Count);
            }

            public virtual void Dispose()
            {
                Context.CurrentRecordChanged -= HandleCurrentRecordChanged;
                Context.Dispose();
            }

            protected virtual void OnCurrentRecordChanged()
            {
            }

            private void HandleCurrentRecordChanged(object sender, CurrentRecordChangedEventArgs<DemoGisRecordViewModel> e)
            {
                _notifications++;
                OnCurrentRecordChanged();
            }
        }

        private sealed class EditSessionContextWithRebuildBenchmark : EditSessionCurrentRecordBenchmark
        {
            private readonly string[] _rowIds;
            private int _editedHits;
            private int _invalidHits;

            public EditSessionContextWithRebuildBenchmark(IReadOnlyList<DemoGisRecordViewModel> rows)
                : base(rows)
            {
                _rowIds = rows.Select(row => row.ObjectId).ToArray();
                Context.StartSession(EditSessionScopeKind.Row, SaveMode.Direct, _rowIds[0]);
                Context.MarkRecordModified(_rowIds[0]);
                Context.MarkRecordModified(_rowIds[9]);
                Context.ApplyValidationErrors(
                    _rowIds[17],
                    new Dictionary<string, IReadOnlyCollection<GridValidationError>>(StringComparer.Ordinal)
                    {
                        ["Owner"] = new[]
                        {
                            new GridValidationError("Owner", "Owner is required."),
                        },
                    });
            }

            public override string Name => "EditSessionContext + full row-state rebuild per move";

            public override NavigationSnapshot RunIteration(int transitionsPerIteration)
            {
                var snapshot = base.RunIteration(transitionsPerIteration);
                return new NavigationSnapshot(
                    snapshot.CurrentRecordId,
                    _editedHits,
                    _invalidHits,
                    snapshot.NotificationCount,
                    snapshot.ProjectedRecordCount);
            }

            protected override void OnCurrentRecordChanged()
            {
                var projection = Context.SurfaceStateProjection;
                var editedHits = 0;
                var invalidHits = 0;
                foreach (var rowId in _rowIds)
                {
                    if (!projection.RecordStates.TryGetValue(rowId, out var state))
                    {
                        continue;
                    }

                    if (state.EditState != RecordEditState.Unchanged)
                    {
                        editedHits++;
                    }

                    if (state.ValidationState == RecordValidationState.Invalid)
                    {
                        invalidHits++;
                    }
                }

                _editedHits = editedHits;
                _invalidHits = invalidHits;
            }
        }

        private sealed class NavigationSnapshot
        {
            public NavigationSnapshot(
                string currentRecordId,
                int editedHits,
                int invalidHits,
                int notificationCount,
                int projectedRecordCount)
            {
                CurrentRecordId = currentRecordId ?? string.Empty;
                EditedHits = editedHits;
                InvalidHits = invalidHits;
                NotificationCount = notificationCount;
                ProjectedRecordCount = projectedRecordCount;
            }

            public string CurrentRecordId { get; }

            public int EditedHits { get; }

            public int InvalidHits { get; }

            public int NotificationCount { get; }

            public int ProjectedRecordCount { get; }
        }

        private sealed class NavigationMeasurement
        {
            public NavigationMeasurement(
                NavigationSnapshot sample,
                double meanMilliseconds,
                double meanAllocatedBytes,
                int transitionsPerIteration)
            {
                Sample = sample;
                MeanMilliseconds = meanMilliseconds;
                MeanAllocatedBytes = meanAllocatedBytes;
                TransitionsPerIteration = transitionsPerIteration;
            }

            public NavigationSnapshot Sample { get; }

            public double MeanMilliseconds { get; }

            public double MeanAllocatedBytes { get; }

            public int TransitionsPerIteration { get; }
        }

        private sealed class DemoGridCellValueProvider : IGridCellValueProvider
        {
            private readonly IReadOnlyDictionary<string, DemoGisRecordViewModel> _rowsById;

            public DemoGridCellValueProvider(IReadOnlyDictionary<string, DemoGisRecordViewModel> rowsById)
            {
                _rowsById = rowsById;
            }

            public bool TryGetValue(string rowKey, string columnKey, out object value)
            {
                value = null;
                if (!_rowsById.TryGetValue(rowKey, out var row))
                {
                    return false;
                }

                switch (columnKey)
                {
                    case "ObjectName":
                        value = row.ObjectName;
                        return true;
                    case "Status":
                        value = row.Status;
                        return true;
                    case "Owner":
                        value = row.Owner;
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}

