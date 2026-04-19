using System;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace PhialeTech.YamlApp.Wpf.Controls.Actions
{
    public sealed class YamlDocumentActionAreaSkiaChromePresenter : SKElement
    {
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(YamlDocumentActionAreaSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(YamlDocumentActionAreaSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(YamlDocumentActionAreaSkiaChromePresenter), new FrameworkPropertyMetadata(new CornerRadius(12d), FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public YamlDocumentActionAreaSkiaChromePresenter()
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

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private static void OnInvalidatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlDocumentActionAreaSkiaChromePresenter)d).InvalidateVisual();
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
            var scale = Math.Min(
                e.Info.Width / Math.Max(0.0001d, ActualWidth),
                e.Info.Height / Math.Max(0.0001d, ActualHeight));

            var topLeft = ScaleRadius(CornerRadius.TopLeft, scale);
            var topRight = ScaleRadius(CornerRadius.TopRight, scale);
            var bottomRight = ScaleRadius(CornerRadius.BottomRight, scale);
            var bottomLeft = ScaleRadius(CornerRadius.BottomLeft, scale);

            using (var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = ToSkColor(BackgroundBrush) })
            using (var borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = ToSkColor(BorderBrush) })
            {
                var roundRect = CreateRoundRect(bounds, topLeft, topRight, bottomRight, bottomLeft);
                canvas.DrawRoundRect(roundRect, fillPaint);

                var inset = borderPaint.StrokeWidth / 2f;
                var borderRect = new SKRect(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
                canvas.DrawRoundRect(
                    CreateRoundRect(
                        borderRect,
                        Math.Max(0f, topLeft - inset),
                        Math.Max(0f, topRight - inset),
                        Math.Max(0f, bottomRight - inset),
                        Math.Max(0f, bottomLeft - inset)),
                    borderPaint);
            }
        }

        private static float ScaleRadius(double radius, double scale)
        {
            return Math.Max(0f, (float)Math.Round(radius * scale));
        }

        private static SKRoundRect CreateRoundRect(SKRect rect, float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            var roundRect = new SKRoundRect();
            roundRect.SetRectRadii(
                rect,
                new[]
                {
                    new SKPoint(topLeft, topLeft),
                    new SKPoint(topRight, topRight),
                    new SKPoint(bottomRight, bottomRight),
                    new SKPoint(bottomLeft, bottomLeft),
                });
            return roundRect;
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
