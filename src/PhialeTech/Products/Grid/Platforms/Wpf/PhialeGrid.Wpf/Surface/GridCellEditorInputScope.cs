using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    internal static class GridCellEditorInputScope
    {
        private static readonly DependencyProperty IsEditorOwnedPopupElementProperty =
            DependencyProperty.RegisterAttached(
                "IsEditorOwnedPopupElement",
                typeof(bool),
                typeof(GridCellEditorInputScope),
                new PropertyMetadata(false));

        public static void SetIsEditorOwnedPopupElement(DependencyObject element, bool value)
        {
            if (element == null)
            {
                return;
            }

            element.SetValue(IsEditorOwnedPopupElementProperty, value);
        }

        public static bool GetIsEditorOwnedPopupElement(DependencyObject element)
        {
            return element != null && element.GetValue(IsEditorOwnedPopupElementProperty) is bool value && value;
        }

        public static bool IsWithinEditorOwnedPopup(DependencyObject source)
        {
            var current = source;
            while (current != null)
            {
                if (GetIsEditorOwnedPopupElement(current))
                {
                    return true;
                }

                current = GetParent(current);
            }

            return false;
        }

        private static DependencyObject GetParent(DependencyObject source)
        {
            if (source == null)
            {
                return null;
            }

            if (source is Visual || source is Visual3D)
            {
                return VisualTreeHelper.GetParent(source);
            }

            return LogicalTreeHelper.GetParent(source);
        }
    }
}
