using NUnit.Framework;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGis.Library.Tests.Grid.Support;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridSummaryEngineTests
    {
        [Test]
        public void Calculate_ProducesCountAvgMinMaxCustom()
        {
            var rows = new[]
            {
                new TestRow { Amount = 10m, Age = 30, Name = "A" },
                new TestRow { Amount = 20m, Age = 20, Name = "B" },
                new TestRow { Amount = 40m, Age = 40, Name = "C" },
            };

            var descriptors = new[]
            {
                new GridSummaryDescriptor("Amount", GridSummaryType.Count),
                new GridSummaryDescriptor("Amount", GridSummaryType.Average),
                new GridSummaryDescriptor("Age", GridSummaryType.Min),
                new GridSummaryDescriptor("Age", GridSummaryType.Max),
                new GridSummaryDescriptor("Name", GridSummaryType.Custom, values => string.Join("", values)),
            };

            var result = GridSummaryEngine.Calculate(rows, descriptors, new DelegateGridRowAccessor<TestRow>((r, c) =>
            {
                switch (c)
                {
                    case "Amount": return r.Amount;
                    case "Age": return r.Age;
                    case "Name": return r.Name;
                    default: return null;
                }
            }));

            Assert.That(result["Amount:Count"], Is.EqualTo(3));
            Assert.That(result["Amount:Average"], Is.EqualTo(70m / 3m));
            Assert.That(result["Age:Min"], Is.EqualTo(20));
            Assert.That(result["Age:Max"], Is.EqualTo(40));
            Assert.That(result["Name:Custom"], Is.EqualTo("ABC"));
        }
    }
}

