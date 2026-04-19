using System;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace PhialeTech.YamlApp.Wpf.Controls.Actions
{
    public sealed class YamlDocumentActionButtonSkiaChromePresenter : SKElement
    {
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HoverBackgroundBrushProperty =
            DependencyProperty.Register(nameof(HoverBackgroundBrush), typeof(Brush), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HoverBorderBrushProperty =
            DependencyProperty.Register(nameof(HoverBorderBrush), typeof(Brush), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty FocusBrushProperty =
            DependencyProperty.Register(nameof(FocusBrush), typeof(Brush), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register(nameof(IsHovered), typeof(bool), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.Register(nameof(IsPressed), typeof(bool), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HasKeyboardFocusProperty =
            DependencyProperty.Register(nameof(HasKeyboardFocus), typeof(bool), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty IsEnabledStateProperty =
            DependencyProperty.Register(nameof(IsEnabledState), typeof(bool), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(YamlDocumentActionButtonSkiaChromePresenter), new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public YamlDocumentActionButtonSkiaChromePresenter()
        {
            PaintSurface += OnPaintSurface;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
        }

        public Brush BackgroundBrush
        {
            get => (Brush)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Brush HoverBackgroundBrush
        {
            get => (Brush)GetValue(HoverBackgroundBrushProperty);
            set => SetValue(HoverBackgroundBrushProperty, value);
        }

        public Brush HoverBorderBrush
        {
            get => (Brush)GetValue(HoverBorderBrushProperty);
            set => SetValue(HoverBorderBrushProperty, value);
        }

        public Brush FocusBrush
        {
            get => (Brush)GetValue(FocusBrushProperty);
            set => SetValue(FocusBrushProperty, value);
        }

        public bool IsHovered
        {
            get => (bool)GetValue(IsHoveredProperty);
            set => SetValue(IsHoveredProperty, value);
        }

        public bool IsPressed
        {
            get => (bool)GetValue(IsPressedProperty);
            set => SetValue(IsPressedProperty, value);
        }

        public bool HasKeyboardFocus
        {
            get => (bool)GetValue(HasKeyboardFocusProperty);
            set => SetValue(HasKeyboardFocusProperty, value);
        }

        public bool IsEnabledState
        {
            get => (bool)GetValue(IsEnabledStateProperty);
            set => SetValue(IsEnabledStateProperty, value);
        }

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private static void OnInvalidatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentActionButtonSkiaChromePresenter)d).InvalidateVisual();
        }

        private void OnPaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (ActualWidth <= 0d || ActualHeight <= 0d)
            {
                return;
            }

            var bounds = new SKRect(0f, 0f, e.Info.Width, e.Info.Height);
            var radius = Math.Max(1f, (float)Math.Round(CornerRadius * Math.Min(
                e.Info.Width / Math.Max(0.0001d, ActualWidth),
                e.Info.Height / Math.Max(0.0001d, ActualHeight))));

            var fill = IsPressed || IsHovered
                ? ToSkColor(HoverBackgroundBrush)
                : ToSkColor(BackgroundBrush);
            var border = HasKeyboardFocus
                ? ToSkColor(FocusBrush)
                : IsPressed || IsHovered
                    ? ToSkColor(HoverBorderBrush)
                    : ToSkColor(BorderBrush);

            if (!IsEnabledState)
            {
                fill = fill.WithAlpha((byte)Math.Min((int)fill.Alpha, 180));
                border = border.WithAlpha((byte)Math.Min((int)border.Alpha, 180));
            }

            using (var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
            using (var borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = HasKeyboardFocus ? 2f : 1f, Color = border })
            {
                var roundRect = new SKRoundRect(bounds, radius, radius);
                canvas.DrawRoundRect(roundRect, fillPaint);

                var inset = borderPaint.StrokeWidth / 2f;
                var borderRect = new SKRect(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
                canvas.DrawRoundRect(new SKRoundRect(borderRect, Math.Max(0f, radius - inset), Math.Max(0f, radius - inset)), borderPaint);
            }
        }

        private static SKColor ToSkColor(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                return new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A);
            }

            return SKColors.Transparent;
        }
    }
}
