using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridSummaryAccumulatorTests
    {
        [Test]
        public void RollingAccumulator_MatchesSummaryEngine()
        {
            var rows = new[]
            {
                new TestRow { Amount = 10m, Age = 30, Name = "A" },
                new TestRow { Amount = 20m, Age = 20, Name = "B" },
                new TestRow { Amount = 40m, Age = 40, Name = "C" },
            };
            var accessor = new DelegateGridRowAccessor<TestRow>((row, columnId) =>
            {
                switch (columnId)
                {
                    case "Amount":
                        return row.Amount;
                    case "Age":
                        return row.Age;
                    case "Name":
                        return row.Name;
                    default:
                        return null;
                }
            });
            var descriptors = new[]
            {
                new GridSummaryDescriptor("Amount", GridSummaryType.Count),
                new GridSummaryDescriptor("Amount", GridSummaryType.Sum),
                new GridSummaryDescriptor("Amount", GridSummaryType.Average),
                new GridSummaryDescriptor("Age", GridSummaryType.Min),
                new GridSummaryDescriptor("Age", GridSummaryType.Max),
                new GridSummaryDescriptor("Name", GridSummaryType.Custom, values => string.Join("", values)),
            };

            var expected = GridSummaryEngine.Calculate(rows, descriptors, accessor);
            var accumulator = GridSummaryEngine.CreateAccumulator(descriptors, null);
            foreach (var row in rows)
            {
                accumulator.AddValue("Amount", accessor.GetValue(row, "Amount"));
                accumulator.AddValue("Age", accessor.GetValue(row, "Age"));
                accumulator.AddValue("Name", accessor.GetValue(row, "Name"));
            }

            var actual = accumulator.ToSummarySet();

            Assert.That(actual["Amount:Count"], Is.EqualTo(expected["Amount:Count"]));
            Assert.That(actual["Amount:Sum"], Is.EqualTo(expected["Amount:Sum"]));
            Assert.That(actual["Amount:Average"], Is.EqualTo(expected["Amount:Average"]));
            Assert.That(actual["Age:Min"], Is.EqualTo(expected["Age:Min"]));
            Assert.That(actual["Age:Max"], Is.EqualTo(expected["Age:Max"]));
            Assert.That(actual["Name:Custom"], Is.EqualTo(expected["Name:Custom"]));
        }
    }
}

