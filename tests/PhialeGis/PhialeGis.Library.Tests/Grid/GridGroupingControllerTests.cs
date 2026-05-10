using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Query;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridGroupingControllerTests
    {
        [Test]
        public void ApplyDrop_AddsNewGroupedColumn()
        {
            var controller = new GridGroupingController();
            var result = controller.ApplyDrop(
                current: System.Array.Empty<GridGroupDescriptor>(),
                payload: new GridGroupingDragPayload("City"),
                target: GridGroupingDropTarget.GroupingPanel);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ColumnId, Is.EqualTo("City"));
            Assert.That(result[0].Direction, Is.EqualTo(GridSortDirection.Ascending));
        }

        [Test]
        public void ApplyDrop_MovesExistingGroupToNewIndex()
        {
            var controller = new GridGroupingController();
            var current = new[]
            {
                new GridGroupDescriptor("City"),
                new GridGroupDescriptor("Age"),
                new GridGroupDescriptor("Status"),
            };

            var result = controller.ApplyDrop(
                current,
                new GridGroupingDragPayload("Status"),
                GridGroupingDropTarget.GroupingPanel,
                dropIndex: 0);

            Assert.That(result.Select(x => x.ColumnId).ToArray(), Is.EqualTo(new[] { "Status", "City", "Age" }));
        }

        [Test]
        public void ApplyDrop_RemoveGrouping_DeletesDescriptor()
        {
            var controller = new GridGroupingController();
            var current = new[]
            {
                new GridGroupDescriptor("City"),
                new GridGroupDescriptor("Age"),
            };

            var result = controller.ApplyDrop(
                current,
                new GridGroupingDragPayload("City"),
                GridGroupingDropTarget.RemoveGrouping);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ColumnId, Is.EqualTo("Age"));
        }

        [Test]
        public void ToggleDirection_ChangesSortDirectionForGroup()
        {
            var controller = new GridGroupingController();
            var current = new[]
            {
                new GridGroupDescriptor("City", GridSortDirection.Ascending),
            };

            var result = controller.ToggleDirection(current, "City");

            Assert.That(result[0].Direction, Is.EqualTo(GridSortDirection.Descending));
        }
    }
}

