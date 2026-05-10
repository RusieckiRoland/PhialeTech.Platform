using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    /// <summary>
    /// Base WPF container for one-line top or bottom grid workspace bands.
    /// </summary>
    public class PhialeWorkspaceBand : ContentControl
    {
        public static readonly DependencyProperty BandPaddingProperty =
            DependencyProperty.Register(
                nameof(BandPadding),
                typeof(Thickness),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(default(Thickness)));

        public static readonly DependencyProperty BandBackgroundProperty =
            DependencyProperty.Register(
                nameof(BandBackground),
                typeof(Brush),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(null));

        public static readonly DependencyProperty BandBorderBrushProperty =
            DependencyProperty.Register(
                nameof(BandBorderBrush),
                typeof(Brush),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(null));

        public static readonly DependencyProperty BandBorderThicknessProperty =
            DependencyProperty.Register(
                nameof(BandBorderThickness),
                typeof(Thickness),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(default(Thickness)));

        public static readonly DependencyProperty BandCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(BandCornerRadius),
                typeof(CornerRadius),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(default(CornerRadius)));

        public static readonly DependencyProperty ToggleTextProperty =
            DependencyProperty.Register(
                nameof(ToggleText),
                typeof(string),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsToggleVisibleProperty =
            DependencyProperty.Register(
                nameof(IsToggleVisible),
                typeof(bool),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsCloseVisibleProperty =
            DependencyProperty.Register(
                nameof(IsCloseVisible),
                typeof(bool),
                typeof(PhialeWorkspaceBand),
                new PropertyMetadata(false));

        public Thickness BandPadding
        {
            get => (Thickness)GetValue(BandPaddingProperty);
            set => SetValue(BandPaddingProperty, value);
        }

        public Brush BandBackground
        {
            get => (Brush)GetValue(BandBackgroundProperty);
            set => SetValue(BandBackgroundProperty, value);
        }

        public Brush BandBorderBrush
        {
            get => (Brush)GetValue(BandBorderBrushProperty);
            set => SetValue(BandBorderBrushProperty, value);
        }

        public Thickness BandBorderThickness
        {
            get => (Thickness)GetValue(BandBorderThicknessProperty);
            set => SetValue(BandBorderThicknessProperty, value);
        }

        public CornerRadius BandCornerRadius
        {
            get => (CornerRadius)GetValue(BandCornerRadiusProperty);
            set => SetValue(BandCornerRadiusProperty, value);
        }

        public string ToggleText
        {
            get => (string)GetValue(ToggleTextProperty);
            set => SetValue(ToggleTextProperty, value);
        }

        public bool IsToggleVisible
        {
            get => (bool)GetValue(IsToggleVisibleProperty);
            set => SetValue(IsToggleVisibleProperty, value);
        }

        public bool IsCloseVisible
        {
            get => (bool)GetValue(IsCloseVisibleProperty);
            set => SetValue(IsCloseVisibleProperty, value);
        }
    }
}
