using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.Styles.Wpf
{
    /// <summary>
    /// Clips a hosted child to the owning border's rounded shape so inner editors
    /// do not paint rectangular corners over the shell chrome.
    /// </summary>
    public static class RoundedChildClipBehavior
    {
        public static readonly DependencyProperty ClipChildToBorderProperty =
            DependencyProperty.RegisterAttached(
                "ClipChildToBorder",
                typeof(bool),
                typeof(RoundedChildClipBehavior),
                new PropertyMetadata(false, OnClipChildToBorderChanged));

        public static bool GetClipChildToBorder(Border border)
        {
            if (border == null)
            {
                throw new ArgumentNullException(nameof(border));
            }

            return (bool)border.GetValue(ClipChildToBorderProperty);
        }

        public static void SetClipChildToBorder(Border border, bool value)
        {
            if (border == null)
            {
                throw new ArgumentNullException(nameof(border));
            }

            border.SetValue(ClipChildToBorderProperty, value);
        }

        public static void UpdateChildClip(Border border)
        {
            if (border == null)
            {
                return;
            }

            if (!(border.Child is UIElement child))
            {
                return;
            }

            if (!GetClipChildToBorder(border))
            {
                child.Clip = null;
                return;
            }

            var size = child.RenderSize;
            if (size.Width <= 0d || size.Height <= 0d)
            {
                child.Clip = null;
                return;
            }

            var radius = GetMaxCornerRadius(border.CornerRadius);
            if (!(child.Clip is RectangleGeometry geometry))
            {
                geometry = new RectangleGeometry();
                child.Clip = geometry;
            }

            geometry.Rect = new Rect(0d, 0d, size.Width, size.Height);
            geometry.RadiusX = radius;
            geometry.RadiusY = radius;
        }

        private static void OnClipChildToBorderChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (!(dependencyObject is Border border))
            {
                return;
            }

            border.Loaded -= HandleBorderLoaded;
            border.SizeChanged -= HandleBorderSizeChanged;

            if (args.NewValue is bool enabled && enabled)
            {
                border.Loaded += HandleBorderLoaded;
                border.SizeChanged += HandleBorderSizeChanged;
                UpdateChildClip(border);
                return;
            }

            if (border.Child is UIElement child)
            {
                child.Clip = null;
            }
        }

        private static void HandleBorderLoaded(object sender, RoutedEventArgs args)
        {
            UpdateChildClip(sender as Border);
        }

        private static void HandleBorderSizeChanged(object sender, SizeChangedEventArgs args)
        {
            UpdateChildClip(sender as Border);
        }

        private static double GetMaxCornerRadius(CornerRadius cornerRadius)
        {
            return Math.Max(
                Math.Max(cornerRadius.TopLeft, cornerRadius.TopRight),
                Math.Max(cornerRadius.BottomRight, cornerRadius.BottomLeft));
        }
    }
}
