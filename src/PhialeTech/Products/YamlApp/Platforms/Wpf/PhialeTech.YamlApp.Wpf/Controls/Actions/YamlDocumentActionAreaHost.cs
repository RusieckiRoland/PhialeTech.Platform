using System.Windows;
using System.Windows.Controls;
using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Wpf.Controls.Actions
{
    public sealed class YamlDocumentActionAreaHost : ContentControl
    {
        public static readonly DependencyProperty AlignmentModeProperty =
            DependencyProperty.Register(
                nameof(AlignmentMode),
                typeof(ActionAlignment),
                typeof(YamlDocumentActionAreaHost),
                new FrameworkPropertyMetadata(ActionAlignment.Right));

        public static readonly DependencyProperty IsSharedAreaProperty =
            DependencyProperty.Register(
                nameof(IsSharedArea),
                typeof(bool),
                typeof(YamlDocumentActionAreaHost),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(ActionPlacement),
                typeof(YamlDocumentActionAreaHost),
                new FrameworkPropertyMetadata(ActionPlacement.Top));

        public static readonly DependencyProperty ChromeModeProperty =
            DependencyProperty.Register(
                nameof(ChromeMode),
                typeof(ActionAreaChromeMode),
                typeof(YamlDocumentActionAreaHost),
                new FrameworkPropertyMetadata(ActionAreaChromeMode.Explicit));

        public static readonly DependencyProperty IsStickyProperty =
            DependencyProperty.Register(
                nameof(IsSticky),
                typeof(bool),
                typeof(YamlDocumentActionAreaHost),
                new FrameworkPropertyMetadata(false));

        static YamlDocumentActionAreaHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(YamlDocumentActionAreaHost), new FrameworkPropertyMetadata(typeof(YamlDocumentActionAreaHost)));
        }

        public ActionAlignment AlignmentMode
        {
            get => (ActionAlignment)GetValue(AlignmentModeProperty);
            set => SetValue(AlignmentModeProperty, value);
        }

        public bool IsSharedArea
        {
            get => (bool)GetValue(IsSharedAreaProperty);
            set => SetValue(IsSharedAreaProperty, value);
        }

        public ActionPlacement Placement
        {
            get => (ActionPlacement)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public ActionAreaChromeMode ChromeMode
        {
            get => (ActionAreaChromeMode)GetValue(ChromeModeProperty);
            set => SetValue(ChromeModeProperty, value);
        }

        public bool IsSticky
        {
            get => (bool)GetValue(IsStickyProperty);
            set => SetValue(IsStickyProperty, value);
        }
    }
}
