using System.Windows;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Wpf.Controls.TextBox;

namespace PhialeTech.YamlApp.Wpf.Document
{
    internal static class YamlWpfPresentationHelper
    {
        public static void ApplyPresentation(FrameworkElement element, double? width, FieldWidthHint? widthHint, bool visible, bool enabled)
        {
            if (element == null)
            {
                return;
            }

            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            element.IsEnabled = enabled;

            if (width.HasValue)
            {
                ApplyExactWidth(element, width.Value);
                return;
            }

            if (widthHint.HasValue)
            {
                ApplyWidthHint(element, widthHint.Value);
                return;
            }

            ClearWidthConstraints(element);
        }

        private static void ApplyExactWidth(FrameworkElement element, double width)
        {
            element.Width = width;
            element.ClearValue(FrameworkElement.MinWidthProperty);
            element.ClearValue(FrameworkElement.MaxWidthProperty);
            element.HorizontalAlignment = HorizontalAlignment.Left;
        }

        private static void ApplyWidthHint(FrameworkElement element, FieldWidthHint widthHint)
        {
            var token = ResolveFieldWidthToken(element, widthHint);
            element.MinWidth = token.MinWidth;

            if (token.MaxWidth.HasValue)
            {
                element.MaxWidth = token.MaxWidth.Value;
            }
            else
            {
                element.ClearValue(FrameworkElement.MaxWidthProperty);
            }

            if (token.Stretch)
            {
                element.ClearValue(FrameworkElement.WidthProperty);
                element.HorizontalAlignment = HorizontalAlignment.Stretch;
                return;
            }

            if (token.PreferredWidth.HasValue)
            {
                element.Width = token.PreferredWidth.Value;
            }
            else
            {
                element.ClearValue(FrameworkElement.WidthProperty);
            }

            element.HorizontalAlignment = HorizontalAlignment.Left;
        }

        private static void ClearWidthConstraints(FrameworkElement element)
        {
            element.ClearValue(FrameworkElement.WidthProperty);
            element.ClearValue(FrameworkElement.MinWidthProperty);
            element.ClearValue(FrameworkElement.MaxWidthProperty);
            element.ClearValue(FrameworkElement.HorizontalAlignmentProperty);
        }

        private static FieldWidthTokenDefinition ResolveFieldWidthToken(FrameworkElement element, FieldWidthHint widthHint)
        {
            var key = string.Format("FieldWidth.{0}", widthHint);
            if (element.TryFindResource(key) is FieldWidthTokenDefinition token)
            {
                return token;
            }

            switch (widthHint)
            {
                case FieldWidthHint.Short:
                    return new FieldWidthTokenDefinition { MinWidth = 80d, PreferredWidth = 120d, MaxWidth = 160d, Stretch = false };
                case FieldWidthHint.Medium:
                    return new FieldWidthTokenDefinition { MinWidth = 140d, PreferredWidth = 220d, MaxWidth = 320d, Stretch = false };
                case FieldWidthHint.Long:
                    return new FieldWidthTokenDefinition { MinWidth = 220d, PreferredWidth = 360d, MaxWidth = 520d, Stretch = false };
                case FieldWidthHint.Fill:
                    return new FieldWidthTokenDefinition { MinWidth = 220d, PreferredWidth = 360d, MaxWidth = null, Stretch = true };
                default:
                    return new FieldWidthTokenDefinition { MinWidth = 140d, PreferredWidth = 220d, MaxWidth = 320d, Stretch = false };
            }
        }
    }
}
