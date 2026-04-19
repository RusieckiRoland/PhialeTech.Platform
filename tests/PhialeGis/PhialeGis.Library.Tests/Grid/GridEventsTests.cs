using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Input;
using PhialeGrid.Core.Query;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridEventsTests
    {
        [Test]
        public void RaiseMethods_InvokeEvents()
        {
            var eventsHub = new GridEvents();
            var called = 0;

            eventsHub.CellPreparing += (_, __) => called++;
            eventsHub.Sorting += (_, __) => called++;
            eventsHub.Filtering += (_, __) => called++;

            eventsHub.RaiseCellPreparing(new GridCellPosition(0, 0));
            eventsHub.RaiseSorting(new[] { new GridSortDescriptor("Name", GridSortDirection.Ascending) });
            eventsHub.RaiseFiltering(GridFilterGroup.EmptyAnd());

            Assert.That(called, Is.EqualTo(3));
        }
    }
}
