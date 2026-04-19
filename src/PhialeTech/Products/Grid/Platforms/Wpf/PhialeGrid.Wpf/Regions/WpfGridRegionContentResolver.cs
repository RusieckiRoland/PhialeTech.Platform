using System.Windows;
using System.Windows.Controls;

namespace PhialeTech.PhialeGrid.Wpf.Regions
{
    internal static class WpfGridRegionContentResolver
    {
        internal static bool HasRenderableContent(ContentPresenter presenter)
        {
            if (presenter == null || presenter.Content == null)
            {
                return false;
            }

            var element = presenter.Content as UIElement;
            return element == null || element.Visibility == Visibility.Visible;
        }
    }
}
