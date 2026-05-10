using NUnit.Framework;
using PhialeGrid.Core.Virtualization;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridViewportTests
    {
        [Test]
        public void CalculateVisibleRows_ComputesRangeWithOverscan()
        {
            var viewport = new GridViewport(
                horizontalOffset: 0,
                verticalOffset: 45,
                viewportWidth: 300,
                viewportHeight: 100,
                rowHeight: 20,
                columnWidths: new[] { 100d, 100d, 100d });

            var range = viewport.CalculateVisibleRows(totalRows: 100, overscan: 2);

            Assert.That(range.Start, Is.EqualTo(0));
            Assert.That(range.Length, Is.EqualTo(9));
        }

        [Test]
        public void CalculateVisibleColumns_ComputesRangeForVariableWidths()
        {
            var viewport = new GridViewport(
                horizontalOffset: 130,
                verticalOffset: 0,
                viewportWidth: 220,
                viewportHeight: 200,
                rowHeight: 25,
                columnWidths: new[] { 80d, 100d, 120d, 120d, 120d });

            var range = viewport.CalculateVisibleColumns(overscan: 1);

            Assert.That(range.Start, Is.EqualTo(0));
            Assert.That(range.Length, Is.EqualTo(5));
        }
    }
}

