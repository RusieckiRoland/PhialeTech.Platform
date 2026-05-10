using NUnit.Framework;
using PhialeGrid.Core.Query;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests.Grid
{
    public sealed class GridSortInteractionControllerTests
    {
        [Test]
        public void ToggleSort_WithoutShift_ShouldReplaceExistingSorts()
        {
            var controller = new GridSortInteractionController();
            var current = new[]
            {
                new GridSortDescriptor("Category", GridSortDirection.Ascending),
                new GridSortDescriptor("Status", GridSortDirection.Descending),
            };

            var next = controller.ToggleSort(current, "Priority", UniversalModifierKeys.None);

            Assert.That(next, Has.Count.EqualTo(1));
            Assert.That(next[0].ColumnId, Is.EqualTo("Priority"));
            Assert.That(next[0].Direction, Is.EqualTo(GridSortDirection.Ascending));
        }

        [Test]
        public void ToggleSort_WithShift_ShouldPreserveOtherSorts()
        {
            var controller = new GridSortInteractionController();
            var current = new[]
            {
                new GridSortDescriptor("Category", GridSortDirection.Ascending),
                new GridSortDescriptor("Status", GridSortDirection.Descending),
            };

            var next = controller.ToggleSort(current, "Priority", UniversalModifierKeys.Shift);

            Assert.That(next, Has.Count.EqualTo(3));
            Assert.That(next[0].ColumnId, Is.EqualTo("Category"));
            Assert.That(next[1].ColumnId, Is.EqualTo("Status"));
            Assert.That(next[2].ColumnId, Is.EqualTo("Priority"));
        }

        [Test]
        public void ToggleSort_ForExistingColumn_ShouldFlipDirection()
        {
            var controller = new GridSortInteractionController();
            var current = new[]
            {
                new GridSortDescriptor("Category", GridSortDirection.Ascending),
            };

            var next = controller.ToggleSort(current, "Category", UniversalModifierKeys.None);

            Assert.That(next, Has.Count.EqualTo(1));
            Assert.That(next[0].ColumnId, Is.EqualTo("Category"));
            Assert.That(next[0].Direction, Is.EqualTo(GridSortDirection.Descending));
        }
    }
}

