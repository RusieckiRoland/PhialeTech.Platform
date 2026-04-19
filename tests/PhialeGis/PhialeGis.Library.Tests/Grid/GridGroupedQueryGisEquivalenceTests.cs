using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeTech.Components.Shared.Model;
using PhialeTech.Components.Shared.Services;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridGroupedQueryGisEquivalenceTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void OptimizedGroupedWindow_MatchesLegacyPipeline_OnGisDataset(bool collapseAllAfterDiscovery)
        {
            var rows = DemoGisDataLoader.LoadDefaultRecords();
            var engine = CreateEngine();
            var groups = collapseAllAfterDiscovery
                ? new[] { new GridGroupDescriptor("Category") }
                : new[] { new GridGroupDescriptor("District"), new GridGroupDescriptor("Status") };
            var sorts = collapseAllAfterDiscovery
                ? (IReadOnlyList<GridSortDescriptor>)Array.Empty<GridSortDescriptor>()
                : new[] { new GridSortDescriptor("LastInspection", GridSortDirection.Descending) };
            var filterGroup = collapseAllAfterDiscovery
                ? GridFilterGroup.EmptyAnd()
                : new GridFilterGroup(new[] { new GridFilterDescriptor("Municipality", GridFilterOperator.Contains, "Wro") }, GridLogicalOperator.And);
            var summaries = collapseAllAfterDiscovery
                ? (IReadOnlyList<GridSummaryDescriptor>)new[]
                {
                    new GridSummaryDescriptor("AreaSquareMeters", GridSummaryType.Sum),
                    new GridSummaryDescriptor("LengthMeters", GridSummaryType.Sum),
                    new GridSummaryDescriptor("ObjectId", GridSummaryType.Count),
                }
                : new[]
                {
                    new GridSummaryDescriptor("AreaSquareMeters", GridSummaryType.Sum),
                    new GridSummaryDescriptor("LengthMeters", GridSummaryType.Sum),
                };
            var expansion = BuildExpansionState(rows, engine, groups, collapseAllAfterDiscovery);
            var effectiveSorts = BuildEffectiveSorts(sorts, groups);
            var legacy = engine.Execute(rows, new GridQueryRequest(0, rows.Count, effectiveSorts, filterGroup, groups, summaries));
            var legacyGroups = engine.BuildGroupedView(legacy.Items, groups, expansion);
            var legacyWindow = GridGroupFlatRowWindowBuilder.BuildWindow(legacyGroups, 24, 64);
            var optimized = engine.ExecuteGroupedWindow(rows, new GridGroupedQueryRequest(24, 64, sorts, filterGroup, groups, summaries, expansion));

            Assert.That(optimized.VisibleRowCount, Is.EqualTo(legacyWindow.TotalRowCount));
            Assert.That(optimized.TotalItemCount, Is.EqualTo(legacy.TotalCount));
            Assert.That(optimized.TopLevelGroupCount, Is.EqualTo(legacyGroups.Count));
            Assert.That(optimized.GroupIds, Is.EqualTo(FlattenGroupIds(legacyGroups)));
            Assert.That(ToShape(optimized.Rows), Is.EqualTo(ToShape(legacyWindow.Rows)));
            Assert.That(ToSummaryShape(optimized.Summary), Is.EqualTo(ToSummaryShape(legacy.Summary)));
        }

        private static GridGroupExpansionState BuildExpansionState(
            IReadOnlyList<DemoGisRecordViewModel> rows,
            GridQueryEngine<DemoGisRecordViewModel> engine,
            IReadOnlyList<GridGroupDescriptor> groups,
            bool collapseAllAfterDiscovery)
        {
            var expansion = new GridGroupExpansionState();
            if (!collapseAllAfterDiscovery)
            {
                return expansion;
            }

            var builtGroups = engine.BuildGroupedView(rows, groups, expansion);
            foreach (var groupId in FlattenGroupIds(builtGroups))
            {
                expansion.SetExpanded(groupId, false);
            }

            return expansion;
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

        private static GridQueryEngine<DemoGisRecordViewModel> CreateEngine()
        {
            return new GridQueryEngine<DemoGisRecordViewModel>(new DelegateGridRowAccessor<DemoGisRecordViewModel>((row, columnId) =>
            {
                switch (columnId)
                {
                    case "Id":
                    case "ObjectId": return row.ObjectId;
                    case "Category": return row.Category;
                    case "ObjectName": return row.ObjectName;
                    case "GeometryType": return row.GeometryType;
                    case "Municipality": return row.Municipality;
                    case "District": return row.District;
                    case "Status": return row.Status;
                    case "Priority": return row.Priority;
                    case "AreaSquareMeters": return row.AreaSquareMeters;
                    case "LengthMeters": return row.LengthMeters;
                    case "LastInspection": return row.LastInspection;
                    case "Owner": return row.Owner;
                    default: return null;
                }
            }));
        }

        private static string[] ToShape(IReadOnlyList<GridGroupFlatRow<DemoGisRecordViewModel>> rows)
        {
            return rows.Select(row => row.Kind == GridGroupFlatRowKind.GroupHeader
                ? $"H:{row.Level}:{row.GroupColumnId}:{row.GroupKey}:{row.GroupItemCount}:{row.IsExpanded}"
                : $"D:{row.Level}:{row.Item.ObjectId}").ToArray();
        }

        private static string[] ToSummaryShape(GridSummarySet summary)
        {
            return summary.Values
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => entry.Key + "=" + (Convert.ToString(entry.Value, CultureInfo.InvariantCulture) ?? string.Empty))
                .ToArray();
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
    }
}
