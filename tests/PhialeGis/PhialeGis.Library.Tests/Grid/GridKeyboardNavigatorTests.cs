using NUnit.Framework;
using PhialeGrid.Core.Input;
using PhialeGrid.Core.Navigation;

namespace PhialeGis.Library.Tests.Grid
{
    public class GridKeyboardNavigatorTests
    {
        [Test]
        public void Move_ClampsAtBoundaries()
        {
            var navigator = new GridKeyboardNavigator(10, 5);
            var start = new GridCellPosition(0, 0);

            var left = navigator.Move(start, GridNavigationKey.Left);
            var up = navigator.Move(start, GridNavigationKey.Up);
            var far = navigator.Move(new GridCellPosition(9, 4), GridNavigationKey.Right);

            Assert.That(left, Is.EqualTo(new GridCellPosition(0, 0)));
            Assert.That(up, Is.EqualTo(new GridCellPosition(0, 0)));
            Assert.That(far, Is.EqualTo(new GridCellPosition(9, 4)));
        }

        [Test]
        public void Move_TabAndEnterNavigateLikeSpreadsheet()
        {
            var navigator = new GridKeyboardNavigator(10, 5);
            var start = new GridCellPosition(2, 2);

            Assert.That(navigator.Move(start, GridNavigationKey.Tab), Is.EqualTo(new GridCellPosition(2, 3)));
            Assert.That(navigator.Move(start, GridNavigationKey.Enter), Is.EqualTo(new GridCellPosition(3, 2)));
        }
    }
}

