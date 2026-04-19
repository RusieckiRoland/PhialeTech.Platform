using System.Windows;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Controls.TextBox;

namespace PhialeTech.YamlApp.Wpf.Controls.TextBox
{
    internal static class YamlTextBoxDensityMetricsResolver
    {
        public static YamlTextBoxDensityVisualMetrics Resolve(YamlTextBoxChromeState state)
        {
            var density = state?.DensityMode ?? DensityMode.Normal;
            var inline = state != null && state.UsesInlineChrome;
            var leftCaption = state != null && state.CaptionPlacement == CaptionPlacement.Left;

            return inline ? ResolveInline(density, leftCaption) : ResolveFramed(density, leftCaption);
        }

        private static YamlTextBoxDensityVisualMetrics ResolveFramed(DensityMode density, bool leftCaption)
        {
            switch (density)
            {
                case DensityMode.Compact:
                    return new YamlTextBoxDensityVisualMetrics
                    {
                        LayoutMetrics = new YamlTextBoxLayoutMetrics(86, leftCaption ? 58 : 14, 28, 14, 22, leftCaption ? 38 : 0, 16, 16),
                        EditorPadding = new Thickness(8, 0, 24, 1),
                        PlaceholderMargin = new Thickness(9, 0, 28, 0),
                        ClearButtonMargin = new Thickness(0, 0, 8, 0),
                    };
                case DensityMode.Comfortable:
                    return new YamlTextBoxDensityVisualMetrics
                    {
                        LayoutMetrics = new YamlTextBoxLayoutMetrics(102, leftCaption ? 66 : 16, 35, 16, 30, leftCaption ? 42 : 0, 20, 20),
                        EditorPadding = new Thickness(11, 1, 30, 2),
                        PlaceholderMargin = new Thickness(12, 0, 34, 0),
                        ClearButtonMargin = new Thickness(0, 0, 10, 0),
                    };
                default:
                    return new YamlTextBoxDensityVisualMetrics
                    {
                        LayoutMetrics = new YamlTextBoxLayoutMetrics(94, leftCaption ? 62 : 14, 32, 14, 26, leftCaption ? 40 : 0, 18, 18),
                        EditorPadding = new Thickness(9, 0, 28, 1),
                        PlaceholderMargin = new Thickness(10, 0, 32, 0),
                        ClearButtonMargin = new Thickness(0, 0, 8, 0),
                    };
            }
        }

        private static YamlTextBoxDensityVisualMetrics ResolveInline(DensityMode density, bool leftCaption)
        {
            switch (density)
            {
                case DensityMode.Compact:
                    return new YamlTextBoxDensityVisualMetrics
                    {
                        LayoutMetrics = new YamlTextBoxLayoutMetrics(90, leftCaption ? 46 : 0, 31, 0, 25, leftCaption ? 38 : 0, 12, 12),
                        EditorPadding = new Thickness(6, 0, 20, 1),
                        PlaceholderMargin = new Thickness(6, 0, 24, 0),
                        ClearButtonMargin = new Thickness(0, 0, 4, 0),
                    };
                case DensityMode.Comfortable:
                    return new YamlTextBoxDensityVisualMetrics
                    {
                        LayoutMetrics = new YamlTextBoxLayoutMetrics(98, leftCaption ? 50 : 0, 33, 0, 28, leftCaption ? 40 : 0, 14, 14),
                        EditorPadding = new Thickness(8, 1, 24, 1),
                        PlaceholderMargin = new Thickness(8, 0, 28, 0),
                        ClearButtonMargin = new Thickness(0, 0, 6, 0),
                    };
                default:
                    return new YamlTextBoxDensityVisualMetrics
                    {
                        LayoutMetrics = new YamlTextBoxLayoutMetrics(94, leftCaption ? 48 : 0, 32, 0, 26, leftCaption ? 40 : 0, 12, 12),
                        EditorPadding = new Thickness(7, 0, 22, 1),
                        PlaceholderMargin = new Thickness(7, 0, 26, 0),
                        ClearButtonMargin = new Thickness(0, 0, 5, 0),
                    };
            }
        }
    }
}
