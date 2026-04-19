using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridGroupedQueryEngineTests
    {
        [Test]
        public void ExecuteGroupedWindow_MatchesLegacyFlattenedRows_ForExpandedGroups()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw", Age = 30, Amount = 10 },
                new TestRow { Id = "2", Name = "Beta", City = "Warsaw", Age = 40, Amount = 20 },
                new TestRow { Id = "3", Name = "Gamma", City = "Berlin", Age = 50, Amount = 30 },
            };

            var groups = new[]
            {
                new GridGroupDescriptor("City"),
                new GridGroupDescriptor("Age"),
            };
            var expansion = new GridGroupExpansionState();
            var request = new GridGroupedQueryRequest(1, 4, System.Array.Empty<GridSortDescriptor>(), GridFilterGroup.EmptyAnd(), groups, new[]
            {
                new GridSummaryDescriptor("Amount", GridSummaryType.Sum),
            }, expansion);

            var engine = CreateEngine();
            var result = engine.ExecuteGroupedWindow(rows, request);
            var legacyGroups = engine.BuildGroupedView(rows, groups, expansion);
            var legacyWindow = GridGroupFlatRowWindowBuilder.BuildWindow(legacyGroups, 1, 4);

            Assert.That(result.VisibleRowCount, Is.EqualTo(legacyWindow.TotalRowCount));
            Assert.That(result.TotalItemCount, Is.EqualTo(rows.Length));
            Assert.That(result.TopLevelGroupCount, Is.EqualTo(2));
            Assert.That(result.Summary["Amount:Sum"], Is.EqualTo(60m));
            Assert.That(ToShape(result.Rows), Is.EqualTo(ToShape(legacyWindow.Rows)));
        }

        [Test]
        public void ExecuteGroupedWindow_RespectsCollapsedState_AndReturnsOnlyVisibleSlice()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw", Age = 30 },
                new TestRow { Id = "2", Name = "Beta", City = "Warsaw", Age = 40 },
                new TestRow { Id = "3", Name = "Gamma", City = "Berlin", Age = 50 },
            };

            var groups = new[] { new GridGroupDescriptor("City") };
            var expansion = new GridGroupExpansionState();
            var engine = CreateEngine();
            var builtGroups = engine.BuildGroupedView(rows, groups, expansion);
            foreach (var group in builtGroups)
            {
                expansion.SetExpanded(group.Id, false);
            }

            var result = engine.ExecuteGroupedWindow(rows, new GridGroupedQueryRequest(0, 10, System.Array.Empty<GridSortDescriptor>(), GridFilterGroup.EmptyAnd(), groups, System.Array.Empty<GridSummaryDescriptor>(), expansion));

            Assert.That(result.VisibleRowCount, Is.EqualTo(2));
            Assert.That(result.Rows.Count, Is.EqualTo(2));
            Assert.That(result.Rows.All(row => row.Kind == GridGroupFlatRowKind.GroupHeader), Is.True);
            Assert.That(result.Rows.All(row => !row.IsExpanded), Is.True);
        }

        [Test]
        public void ExecuteGroupedWindow_WithNoGroups_ReturnsPagedDataRowsOnly()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", Amount = 10 },
                new TestRow { Id = "2", Name = "Beta", Amount = 20 },
                new TestRow { Id = "3", Name = "Gamma", Amount = 30 },
            };

            var result = CreateEngine().ExecuteGroupedWindow(rows, new GridGroupedQueryRequest(
                1,
                1,
                new[] { new GridSortDescriptor("Name", GridSortDirection.Ascending) },
                GridFilterGroup.EmptyAnd(),
                System.Array.Empty<GridGroupDescriptor>(),
                new[] { new GridSummaryDescriptor("Amount", GridSummaryType.Sum) },
                new GridGroupExpansionState()));

            Assert.That(result.VisibleRowCount, Is.EqualTo(3));
            Assert.That(result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Rows[0].Kind, Is.EqualTo(GridGroupFlatRowKind.DataRow));
            Assert.That(result.Rows[0].Item.Name, Is.EqualTo("Beta"));
            Assert.That(result.Summary["Amount:Sum"], Is.EqualTo(60m));
        }

        private static string[] ToShape(System.Collections.Generic.IReadOnlyList<GridGroupFlatRow<TestRow>> rows)
        {
            return rows.Select(row => row.Kind == GridGroupFlatRowKind.GroupHeader
                ? $"H:{row.Level}:{row.GroupColumnId}:{row.GroupKey}:{row.GroupItemCount}:{row.IsExpanded}"
                : $"D:{row.Level}:{row.Item.Id}").ToArray();
        }

        private static GridQueryEngine<TestRow> CreateEngine()
        {
            return new GridQueryEngine<TestRow>(new DelegateGridRowAccessor<TestRow>((row, col) =>
            {
                switch (col)
                {
                    case "Id": return row.Id;
                    case "Name": return row.Name;
                    case "Age": return row.Age;
                    case "City": return row.City;
                    case "Amount": return row.Amount;
                    default: return null;
                }
            }));
        }
    }
}
