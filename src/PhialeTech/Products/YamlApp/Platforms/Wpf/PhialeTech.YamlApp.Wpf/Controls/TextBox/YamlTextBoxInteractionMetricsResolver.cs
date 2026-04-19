using System;
using System.Windows;
using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Core.Controls.TextBox;

namespace PhialeTech.YamlApp.Wpf.Controls.TextBox
{
    internal static class YamlTextBoxInteractionMetricsResolver
    {
        public static YamlTextBoxDensityVisualMetrics Resolve(YamlTextBoxChromeState state, YamlTextBoxDensityVisualMetrics baseProfile)
        {
            if (state == null || baseProfile == null || state.InteractionMode != InteractionMode.Touch)
            {
                return baseProfile;
            }

            var clearButtonSize = state.UsesInlineChrome ? 22d : 24d;
            var clearButtonMargin = new Thickness(0, 0, state.UsesInlineChrome ? 8d : 10d, 0);
            var editorRightInset = clearButtonSize + clearButtonMargin.Right + (state.UsesInlineChrome ? 8d : 10d);

            return new YamlTextBoxDensityVisualMetrics
            {
                LayoutMetrics = new YamlTextBoxLayoutMetrics(
                    baseProfile.LayoutMetrics.MinimumHeight,
                    baseProfile.LayoutMetrics.EditorLeft,
                    baseProfile.LayoutMetrics.EditorTop,
                    Math.Max(baseProfile.LayoutMetrics.EditorRight, editorRightInset),
                    baseProfile.LayoutMetrics.EditorBottom,
                    baseProfile.LayoutMetrics.CaptionWidth,
                    Math.Max(baseProfile.LayoutMetrics.ClearButtonWidth, clearButtonSize),
                    Math.Max(baseProfile.LayoutMetrics.ClearButtonHeight, clearButtonSize)),
                EditorPadding = new Thickness(
                    baseProfile.EditorPadding.Left,
                    baseProfile.EditorPadding.Top,
                    Math.Max(baseProfile.EditorPadding.Right, editorRightInset + 4d),
                    baseProfile.EditorPadding.Bottom),
                PlaceholderMargin = new Thickness(
                    baseProfile.PlaceholderMargin.Left,
                    baseProfile.PlaceholderMargin.Top,
                    Math.Max(baseProfile.PlaceholderMargin.Right, editorRightInset + 8d),
                    baseProfile.PlaceholderMargin.Bottom),
                ClearButtonMargin = clearButtonMargin,
            };
        }
    }
}
