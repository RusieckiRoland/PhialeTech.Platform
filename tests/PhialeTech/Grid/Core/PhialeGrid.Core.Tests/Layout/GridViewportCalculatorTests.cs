using NUnit.Framework;
using PhialeGrid.Core.Layout;

namespace PhialeGrid.Core.Tests.Layout
{
    [TestFixture]
    public class GridViewportCalculatorTests
    {
        [Test]
        public void CalculateRowRange_ReturnsVisibleAndBufferedSegments()
        {
            var sut = new GridViewportCalculator();
            var rows = new[]
            {
                new GridRowLayout { RowKey = "r0", Y = 0, Height = 20 },
                new GridRowLayout { RowKey = "r1", Y = 20, Height = 20 },
                new GridRowLayout { RowKey = "r2", Y = 40, Height = 20 },
                new GridRowLayout { RowKey = "r3", Y = 60, Height = 20 },
                new GridRowLayout { RowKey = "r4", Y = 80, Height = 20 },
            };

            var range = sut.CalculateRowRange(
                verticalOffset: 25,
                viewportHeight: 35,
                rowLayouts: rows,
                bufferBefore: 10,
                bufferAfter: 15);

            Assert.Multiple(() =>
            {
                Assert.That(range.VisibleStart, Is.EqualTo(1));
                Assert.That(range.VisibleEnd, Is.EqualTo(4));
                Assert.That(range.BufferedStart, Is.EqualTo(0));
                Assert.That(range.BufferedEnd, Is.EqualTo(4));
            });
        }

        [Test]
        public void CalculateColumnRange_ClampsBufferedEndToLayoutCount()
        {
            var sut = new GridViewportCalculator();
            var columns = new[]
            {
                new GridColumnLayout { ColumnKey = "c0", X = 0, Width = 50 },
                new GridColumnLayout { ColumnKey = "c1", X = 50, Width = 50 },
                new GridColumnLayout { ColumnKey = "c2", X = 100, Width = 50 },
            };

            var range = sut.CalculateColumnRange(
                horizontalOffset: 90,
                viewportWidth: 30,
                columnLayouts: columns,
                bufferBefore: 10,
                bufferAfter: 100);

            Assert.Multiple(() =>
            {
                Assert.That(range.VisibleStart, Is.EqualTo(1));
                Assert.That(range.VisibleEnd, Is.EqualTo(3));
                Assert.That(range.BufferedStart, Is.EqualTo(1));
                Assert.That(range.BufferedEnd, Is.EqualTo(3));
            });
        }

        [Test]
        public void CalculateRowRange_ForEmptyLayouts_ReturnsEmptyRange()
        {
            var sut = new GridViewportCalculator();

            var range = sut.CalculateRowRange(0, 100, rowLayouts: null);

            Assert.Multiple(() =>
            {
                Assert.That(range.VisibleStart, Is.EqualTo(0));
                Assert.That(range.VisibleEnd, Is.EqualTo(0));
                Assert.That(range.BufferedStart, Is.EqualTo(0));
                Assert.That(range.BufferedEnd, Is.EqualTo(0));
            });
        }
    }
}

