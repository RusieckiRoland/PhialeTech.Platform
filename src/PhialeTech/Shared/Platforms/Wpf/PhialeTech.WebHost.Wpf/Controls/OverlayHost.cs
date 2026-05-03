using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.WebHost.Wpf.Controls
{
    public static class OverlayHost
    {
        public static readonly DependencyProperty IsScopeProperty =
            DependencyProperty.RegisterAttached(
                "IsScope",
                typeof(bool),
                typeof(OverlayHost),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsScope(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return (bool)element.GetValue(IsScopeProperty);
        }

        public static void SetIsScope(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(IsScopeProperty, value);
        }

        public static Panel FindNearestScopePanel(DependencyObject origin)
        {
            var current = origin;
            while (current != null)
            {
                if (current is Panel panel && GetIsScope(panel))
                {
                    return panel;
                }

                if (current is Window)
                {
                    break;
                }

                current = GetParent(current);
            }

            return null;
        }

        internal static DependencyObject GetParent(DependencyObject current)
        {
            if (current == null)
            {
                return null;
            }

            if (current is Visual)
            {
                var visualParent = VisualTreeHelper.GetParent(current);
                if (visualParent != null)
                {
                    return visualParent;
                }
            }

            return LogicalTreeHelper.GetParent(current);
        }

        internal static bool IsDescendantOf(DependencyObject node, DependencyObject ancestor)
        {
            var current = node;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = GetParent(current);
            }

            return false;
        }
    }
}
