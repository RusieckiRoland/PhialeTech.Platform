using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeTech.ActiveLayerSelector;

namespace PhialeTech.ActiveLayerSelector.Wpf.Controls
{
    public sealed class ActiveLayerSvgIcon : Image
    {
        public static readonly DependencyProperty CapabilityProperty = DependencyProperty.Register(
            nameof(Capability),
            typeof(ActiveLayerSelectorCapabilityKind),
            typeof(ActiveLayerSvgIcon),
            new PropertyMetadata(ActiveLayerSelectorCapabilityKind.Visible, HandleIconPropertyChanged));

        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            nameof(IsOn),
            typeof(bool),
            typeof(ActiveLayerSvgIcon),
            new PropertyMetadata(false, HandleIconPropertyChanged));

        public ActiveLayerSelectorCapabilityKind Capability
        {
            get => (ActiveLayerSelectorCapabilityKind)GetValue(CapabilityProperty);
            set => SetValue(CapabilityProperty, value);
        }

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        public ActiveLayerSvgIcon()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
            Source = ActiveLayerSvgIconCache.GetCapabilityImage(Capability, IsOn);
        }

        private static void HandleIconPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((ActiveLayerSvgIcon)dependencyObject).Source = ActiveLayerSvgIconCache.GetCapabilityImage(((ActiveLayerSvgIcon)dependencyObject).Capability, ((ActiveLayerSvgIcon)dependencyObject).IsOn);
        }
    }
}
