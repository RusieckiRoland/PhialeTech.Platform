using NUnit.Framework;
using PhialeGrid.Core.Input;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests.Grid
{
    public sealed class GridHeaderDragControllerTests
    {
        [Test]
        public void UpwardDragBeyondThreshold_ShouldStartGroupingDrag()
        {
            var controller = new GridHeaderDragController();
            controller.BeginHeaderPointerPressed(CreatePointerArgs(160d, 140d, true), "Category");

            var action = controller.HandleHeaderPointerMoved(CreatePointerArgs(164d, 114d, true), 14d);

            Assert.That(action.Kind, Is.EqualTo(GridHeaderDragActionKind.BeginGroupingDrag));
            Assert.That(action.ColumnId, Is.EqualTo("Category"));
        }

        [Test]
        public void HorizontalMovementDominatingVertical_ShouldNotStartDrag()
        {
            var controller = new GridHeaderDragController();
            controller.BeginHeaderPointerPressed(CreatePointerArgs(160d, 140d, true), "Category");

            var action = controller.HandleHeaderPointerMoved(CreatePointerArgs(192d, 126d, true), 14d);

            Assert.That(action.Kind, Is.EqualTo(GridHeaderDragActionKind.None));
        }

        [Test]
        public void ReleasingPointer_ShouldCancelPendingDrag()
        {
            var controller = new GridHeaderDragController();
            controller.BeginHeaderPointerPressed(CreatePointerArgs(160d, 140d, true), "Category");
            controller.HandleHeaderPointerReleased(CreatePointerArgs(160d, 140d, false));

            var action = controller.HandleHeaderPointerMoved(CreatePointerArgs(162d, 110d, true), 14d);

            Assert.That(action.Kind, Is.EqualTo(GridHeaderDragActionKind.None));
        }

        private static UniversalPointerRoutedEventArgs CreatePointerArgs(double x, double y, bool isLeftPressed)
        {
            var pointer = new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = x, Y = y })
            {
                Properties = new UniversalPointerPointProperties
                {
                    IsLeftButtonPressed = isLeftPressed,
                },
            };

            return new UniversalPointerRoutedEventArgs(pointer)
            {
                Metadata = new UniversalMetadata(),
            };
        }
    }
}
