using System;
using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridQueryEngineProjectionTests
    {
        [Test]
        public void Execute_StreamingPath_UsesAccessorAtMostOncePerReferencedColumnPerRow()
        {
            var rows = CreateRows(64);
            var accessor = new CountingAccessor<TestRow>(ResolveValue);
            var engine = new GridQueryEngine<TestRow>(accessor);

            engine.Execute(
                rows,
                new GridQueryRequest(
                    4,
                    12,
                    Array.Empty<GridSortDescriptor>(),
                    new GridFilterGroup(new[]
                    {
                        new GridFilterDescriptor("Active", GridFilterOperator.IsTrue),
                        new GridFilterDescriptor("Name", GridFilterOperator.Contains, "Name"),
                    }, GridLogicalOperator.And),
                    Array.Empty<GridGroupDescriptor>(),
                    new[]
                    {
                        new GridSummaryDescriptor("Amount", GridSummaryType.Sum),
                    }));

            Assert.That(accessor.TotalCalls, Is.LessThanOrEqualTo(rows.Length * 3));
        }

        [Test]
        public void Execute_MaterializedPath_UsesAccessorAtMostOncePerReferencedColumnPerRow()
        {
            var rows = CreateRows(64);
            var accessor = new CountingAccessor<TestRow>(ResolveValue);
            var engine = new GridQueryEngine<TestRow>(accessor);

            engine.Execute(
                rows,
                new GridQueryRequest(
                    0,
                    20,
                    new[]
                    {
                        new GridSortDescriptor("Age", GridSortDirection.Descending),
                        new GridSortDescriptor("Name", GridSortDirection.Ascending),
                    },
                    new GridFilterGroup(new[]
                    {
                        new GridFilterDescriptor("Active", GridFilterOperator.IsTrue),
                        new GridFilterDescriptor("Name", GridFilterOperator.Contains, "Name"),
                    }, GridLogicalOperator.And),
                    new[]
                    {
                        new GridGroupDescriptor("City"),
                    },
                    new[]
                    {
                        new GridSummaryDescriptor("Amount", GridSummaryType.Sum),
                    }));

            Assert.That(accessor.TotalCalls, Is.LessThanOrEqualTo(rows.Length * 5));
        }

        [Test]
        public void ExecuteGroupedWindow_UsesAccessorAtMostOncePerReferencedColumnPerRow()
        {
            var rows = CreateRows(64);
            var accessor = new CountingAccessor<TestRow>(ResolveValue);
            var engine = new GridQueryEngine<TestRow>(accessor);

            engine.ExecuteGroupedWindow(
                rows,
                new GridGroupedQueryRequest(
                    10,
                    24,
                    new[]
                    {
                        new GridSortDescriptor("Name", GridSortDirection.Ascending),
                    },
                    new GridFilterGroup(new[]
                    {
                        new GridFilterDescriptor("Active", GridFilterOperator.IsTrue),
                    }, GridLogicalOperator.And),
                    new[]
                    {
                        new GridGroupDescriptor("City"),
                        new GridGroupDescriptor("Age"),
                    },
                    new[]
                    {
                        new GridSummaryDescriptor("Amount", GridSummaryType.Sum),
                    },
                    new GridGroupExpansionState()));

            Assert.That(accessor.TotalCalls, Is.LessThanOrEqualTo(rows.Length * 5));
        }

        [Test]
        public void BuildGroupedView_UsesAccessorAtMostOncePerGroupingColumnPerRow()
        {
            var rows = CreateRows(64);
            var accessor = new CountingAccessor<TestRow>(ResolveValue);
            var engine = new GridQueryEngine<TestRow>(accessor);

            engine.BuildGroupedView(
                rows,
                new[]
                {
                    new GridGroupDescriptor("City"),
                    new GridGroupDescriptor("Age"),
                },
                new GridGroupExpansionState());

            Assert.That(accessor.TotalCalls, Is.LessThanOrEqualTo(rows.Length * 2));
        }

        private static TestRow[] CreateRows(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new TestRow
                {
                    Id = i.ToString(),
                    Name = "Name " + i,
                    Age = 20 + (i % 10),
                    City = i % 3 == 0 ? "Warsaw" : (i % 3 == 1 ? "Berlin" : "Paris"),
                    Active = i % 2 == 0,
                    Amount = i * 10m,
                })
                .ToArray();
        }

        private static object ResolveValue(TestRow row, string columnId)
        {
            switch (columnId)
            {
                case "Id":
                    return row.Id;
                case "Name":
                    return row.Name;
                case "Age":
                    return row.Age;
                case "City":
                    return row.City;
                case "Active":
                    return row.Active;
                case "Amount":
                    return row.Amount;
                default:
                    return null;
            }
        }

        private sealed class CountingAccessor<TRow> : IGridRowAccessor<TRow>
        {
            private readonly Func<TRow, string, object> _resolver;

            public CountingAccessor(Func<TRow, string, object> resolver)
            {
                _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            }

            public int TotalCalls { get; private set; }

            public object GetValue(TRow row, string columnId)
            {
                TotalCalls++;
                return _resolver(row, columnId);
            }
        }
    }
}
