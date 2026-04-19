using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridGroupRowFlattenerTests
    {
        [Test]
        public void Flatten_CollapsedGroups_ReturnsOnlyHeaders()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw" },
                new TestRow { Id = "2", Name = "Beta", City = "Berlin" },
                new TestRow { Id = "3", Name = "Gamma", City = "Warsaw" },
            };

            var engine = CreateEngine();
            var expansion = new GridGroupExpansionState();
            var groups = engine.BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);

            foreach (var group in groups)
            {
                expansion.SetExpanded(group.Id, false);
            }

            groups = engine.BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);
            var flatRows = GridGroupRowFlattener.Flatten(groups);

            Assert.That(flatRows.Count, Is.EqualTo(2));
            Assert.That(flatRows.All(row => row.Kind == GridGroupFlatRowKind.GroupHeader), Is.True);
        }

        [Test]
        public void Flatten_ExpandedLeafGroup_IncludesDataRows()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw" },
                new TestRow { Id = "2", Name = "Beta", City = "Warsaw" },
            };

            var flatRows = GridGroupRowFlattener.Flatten(
                CreateEngine().BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, new GridGroupExpansionState()));

            Assert.That(flatRows.Select(row => row.Kind).ToArray(), Is.EqualTo(new[]
            {
                GridGroupFlatRowKind.GroupHeader,
                GridGroupFlatRowKind.DataRow,
                GridGroupFlatRowKind.DataRow,
            }));
        }

        [Test]
        public void Flatten_NestedGroups_PreservesHierarchyOrder()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw", Age = 30 },
                new TestRow { Id = "2", Name = "Beta", City = "Warsaw", Age = 40 },
            };

            var groups = CreateEngine().BuildGroupedView(
                rows,
                new[]
                {
                    new GridGroupDescriptor("City"),
                    new GridGroupDescriptor("Age"),
                },
                new GridGroupExpansionState());

            var flatRows = GridGroupRowFlattener.Flatten(groups);

            Assert.That(flatRows[0].Kind, Is.EqualTo(GridGroupFlatRowKind.GroupHeader));
            Assert.That(flatRows[0].Level, Is.EqualTo(0));
            Assert.That(flatRows[1].Kind, Is.EqualTo(GridGroupFlatRowKind.GroupHeader));
            Assert.That(flatRows[1].Level, Is.EqualTo(1));
            Assert.That(flatRows.Last().Kind, Is.EqualTo(GridGroupFlatRowKind.DataRow));
            Assert.That(flatRows.Last().Level, Is.EqualTo(2));
        }

        [Test]
        public void BuildWindow_ReturnsOnlyRequestedSlice_AndPreservesTotalCount()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw" },
                new TestRow { Id = "2", Name = "Beta", City = "Berlin" },
                new TestRow { Id = "3", Name = "Gamma", City = "Paris" },
            };

            var expansion = new GridGroupExpansionState();
            var groups = CreateEngine().BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);
            foreach (var group in groups)
            {
                expansion.SetExpanded(group.Id, false);
            }

            groups = CreateEngine().BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);
            var window = GridGroupFlatRowWindowBuilder.BuildWindow(groups, 1, 2);

            Assert.That(window.TotalRowCount, Is.EqualTo(3));
            Assert.That(window.Rows.Count, Is.EqualTo(2));
            Assert.That(window.Rows[0].Kind, Is.EqualTo(GridGroupFlatRowKind.GroupHeader));
            Assert.That(window.Rows[1].Kind, Is.EqualTo(GridGroupFlatRowKind.GroupHeader));
        }

        [Test]
        public void BuildWindow_ExpandedNestedGroups_IncludesOnlyVisibleSlice()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw", Age = 30 },
                new TestRow { Id = "2", Name = "Beta", City = "Warsaw", Age = 40 },
            };

            var groups = CreateEngine().BuildGroupedView(
                rows,
                new[]
                {
                    new GridGroupDescriptor("City"),
                    new GridGroupDescriptor("Age"),
                },
                new GridGroupExpansionState());

            var window = GridGroupFlatRowWindowBuilder.BuildWindow(groups, 1, 3);

            Assert.That(window.TotalRowCount, Is.EqualTo(5));
            Assert.That(window.Rows.Select(row => row.Kind).ToArray(), Is.EqualTo(new[]
            {
                GridGroupFlatRowKind.GroupHeader,
                GridGroupFlatRowKind.DataRow,
                GridGroupFlatRowKind.GroupHeader,
            }));
            Assert.That(window.Rows[0].Level, Is.EqualTo(1));
            Assert.That(window.Rows[1].Level, Is.EqualTo(2));
        }

        [Test]
        public void BuildWindow_StartBeyondTail_ReturnsEmptyRowsButKeepsCount()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alpha", City = "Warsaw" },
                new TestRow { Id = "2", Name = "Beta", City = "Berlin" },
            };

            var expansion = new GridGroupExpansionState();
            var groups = CreateEngine().BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);
            foreach (var group in groups)
            {
                expansion.SetExpanded(group.Id, false);
            }

            groups = CreateEngine().BuildGroupedView(rows, new[] { new GridGroupDescriptor("City") }, expansion);
            var window = GridGroupFlatRowWindowBuilder.BuildWindow(groups, 10, 4);

            Assert.That(window.TotalRowCount, Is.EqualTo(2));
            Assert.That(window.Rows, Is.Empty);
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
                    default: return null;
                }
            }));
        }
    }
}
