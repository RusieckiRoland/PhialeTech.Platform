using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridQueryEngineTests
    {
        [Test]
        public void Execute_AppliesMultiSortFilterPaging()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alice", Age = 30, City = "Warsaw", Active = true, Amount = 10 },
                new TestRow { Id = "2", Name = "Bob", Age = 35, City = "Berlin", Active = true, Amount = 20 },
                new TestRow { Id = "3", Name = "Aaron", Age = 35, City = "Paris", Active = false, Amount = 30 },
                new TestRow { Id = "4", Name = "Cora", Age = 25, City = "Rome", Active = true, Amount = 40 },
            };

            var engine = CreateEngine();
            var request = new GridQueryRequest(
                offset: 0,
                size: 2,
                sorts: new[]
                {
                    new GridSortDescriptor("Age", GridSortDirection.Descending),
                    new GridSortDescriptor("Name", GridSortDirection.Ascending),
                },
                filterGroup: new GridFilterGroup(new[]
                {
                    new GridFilterDescriptor("Active", GridFilterOperator.IsTrue),
                }, GridLogicalOperator.And),
                groups: new[] { new GridGroupDescriptor("City") },
                summaries: new[] { new GridSummaryDescriptor("Amount", GridSummaryType.Sum) });

            var result = engine.Execute(rows, request);

            Assert.That(result.TotalCount, Is.EqualTo(3));
            Assert.That(result.Items.Select(x => x.Name).ToArray(), Is.EqualTo(new[] { "Bob", "Alice" }));
            Assert.That(result.GroupedItems.Count, Is.EqualTo(3));
            Assert.That(result.Summary["Amount:Sum"], Is.EqualTo(70m));
        }

        [Test]
        public void Execute_AppliesOrFilter()
        {
            var rows = new[]
            {
                new TestRow { Id = "1", Name = "Alice", Age = 30, City = "Warsaw", Active = true, Amount = 10 },
                new TestRow { Id = "2", Name = "Bob", Age = 35, City = "Berlin", Active = true, Amount = 20 },
                new TestRow { Id = "3", Name = "Aaron", Age = 35, City = "Paris", Active = false, Amount = 30 },
            };

            var request = new GridQueryRequest(
                offset: 0,
                size: 10,
                sorts: System.Array.Empty<GridSortDescriptor>(),
                filterGroup: new GridFilterGroup(new[]
                {
                    new GridFilterDescriptor("Name", GridFilterOperator.StartsWith, "Aa"),
                    new GridFilterDescriptor("City", GridFilterOperator.Equals, "Berlin"),
                }, GridLogicalOperator.Or),
                groups: System.Array.Empty<GridGroupDescriptor>(),
                summaries: System.Array.Empty<GridSummaryDescriptor>());

            var result = CreateEngine().Execute(rows, request);
            Assert.That(result.TotalCount, Is.EqualTo(2));
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
                    case "Active": return row.Active;
                    case "Amount": return row.Amount;
                    default: return null;
                }
            }));
        }
    }
}
