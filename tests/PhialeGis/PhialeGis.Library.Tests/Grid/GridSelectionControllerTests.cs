using System.Linq;
using NUnit.Framework;
using PhialeGrid.Core.Input;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridSelectionControllerTests
    {
        [Test]
        public void PointerPressed_WithoutModifiers_SelectsSingleCell()
        {
            var controller = new GridSelectionController();
            var e = CreatePointerArgs(isLeftPressed: true);

            controller.HandlePointerPressed(e, new GridCellPosition(2, 3), isCtrlPressed: false, isShiftPressed: false);

            Assert.That(controller.State.SelectedCells.Count, Is.EqualTo(1));
            Assert.That(controller.State.SelectedCells.Single(), Is.EqualTo(new GridCellPosition(2, 3)));
            Assert.That(controller.State.ActiveCell, Is.EqualTo(new GridCellPosition(2, 3)));
            Assert.That(e.Handled, Is.True);
        }

        [Test]
        public void PointerPressed_WithShift_SelectsRectangularRange()
        {
            var controller = new GridSelectionController();

            controller.HandlePointerPressed(CreatePointerArgs(true), new GridCellPosition(1, 1), false, false);
            controller.HandlePointerPressed(CreatePointerArgs(true), new GridCellPosition(2, 3), false, true);

            Assert.That(controller.State.SelectedCells.Count, Is.EqualTo(6));
            Assert.That(controller.State.SelectedCells.Contains(new GridCellPosition(1, 1)), Is.True);
            Assert.That(controller.State.SelectedCells.Contains(new GridCellPosition(2, 3)), Is.True);
        }

        [Test]
        public void PointerPressed_WithCtrl_TogglesCell()
        {
            var controller = new GridSelectionController();

            controller.HandlePointerPressed(CreatePointerArgs(true), new GridCellPosition(5, 5), false, false);
            controller.HandlePointerPressed(CreatePointerArgs(true), new GridCellPosition(5, 5), true, false);

            Assert.That(controller.State.SelectedCells.Count, Is.EqualTo(0));
        }

        [Test]
        public void DragSelection_ExtendsFromAnchorToCurrentCell()
        {
            var controller = new GridSelectionController();
            controller.HandlePointerPressed(CreatePointerArgs(true), new GridCellPosition(3, 3), false, false);

            controller.HandlePointerMoved(CreatePointerArgs(true), new GridCellPosition(4, 5));
            controller.HandlePointerReleased(CreatePointerArgs(false), new GridCellPosition(4, 5));

            Assert.That(controller.State.SelectedCells.Count, Is.EqualTo(6));
            Assert.That(controller.State.SelectedCells.Contains(new GridCellPosition(3, 3)), Is.True);
            Assert.That(controller.State.SelectedCells.Contains(new GridCellPosition(4, 5)), Is.True);
        }

        private static UniversalPointerRoutedEventArgs CreatePointerArgs(bool isLeftPressed)
        {
            var pointer = new UniversalPointer(DeviceType.Mouse, new UniversalPoint())
            {
                Properties = new UniversalPointerPointProperties
                {
                    IsLeftButtonPressed = isLeftPressed,
                }
            };

            return new UniversalPointerRoutedEventArgs(pointer)
            {
                Handled = false,
                Metadata = null,
            };
        }
    }
}

