using System;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace PhialeTech.YamlApp.Wpf.Controls.Badges
{
    public sealed class YamlBadgeSkiaChromePresenter : SKElement
    {
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(YamlBadgeSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(YamlBadgeSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(YamlBadgeSkiaChromePresenter), new FrameworkPropertyMetadata(999d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public YamlBadgeSkiaChromePresenter()
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

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private static void OnInvalidatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlBadgeSkiaChromePresenter)d).InvalidateVisual();
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

            using (var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = ToSkColor(BackgroundBrush) })
            using (var borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = ToSkColor(BorderBrush) })
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
