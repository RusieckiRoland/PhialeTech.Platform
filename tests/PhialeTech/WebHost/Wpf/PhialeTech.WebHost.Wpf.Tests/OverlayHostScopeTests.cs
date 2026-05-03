using System.Windows.Controls;
using NUnit.Framework;
using PhialeTech.WebHost.Wpf.Controls;

namespace PhialeTech.WebHost.Wpf.Tests
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public sealed class OverlayHostScopeTests
    {
        [Test]
        public void FindNearestScopePanel_ReturnsNearestExplicitScope()
        {
            var root = new Grid();
            var outer = new Grid();
            var inner = new Grid();
            var editor = new Border();

            OverlayHost.SetIsScope(root, true);
            OverlayHost.SetIsScope(inner, true);
            root.Children.Add(outer);
            outer.Children.Add(inner);
            inner.Children.Add(editor);

            var scope = OverlayHost.FindNearestScopePanel(editor);

            Assert.That(scope, Is.SameAs(inner));
        }

        [Test]
        public void FindNearestScopePanel_DoesNotFallbackToAncestorPanel()
        {
            var root = new Grid();
            var cell = new Grid();
            var editor = new Border();

            root.Children.Add(cell);
            cell.Children.Add(editor);

            var scope = OverlayHost.FindNearestScopePanel(editor);

            Assert.That(scope, Is.Null);
        }
    }
}
