using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace PhialeTech.ComponentHost.Wpf.Controls
{
    public sealed class PhialeGlassScrimSkiaPresenter : SKElement
    {
        public static readonly DependencyProperty TintBrushProperty =
            DependencyProperty.Register(nameof(TintBrush), typeof(Brush), typeof(PhialeGlassScrimSkiaPresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(PhialeGlassScrimSkiaPresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public PhialeGlassScrimSkiaPresenter()
        {
            PaintSurface += OnPaintSurface;
        }

        public Brush TintBrush
        {
            get => (Brush)GetValue(TintBrushProperty);
            set => SetValue(TintBrushProperty, value);
        }

        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        private static void OnInvalidatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhialeGlassScrimSkiaPresenter)d).InvalidateVisual();
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
            var tint = ToSkColor(TintBrush);
            var highlight = ToSkColor(HighlightBrush);

            using (var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = tint })
            {
                canvas.DrawRect(bounds, fillPaint);
            }

            using (var highlightPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Shader = SKShader.CreateRadialGradient(
                    new SKPoint(bounds.MidX, bounds.Top - (bounds.Height * 0.05f)),
                    bounds.Width * 0.65f,
                    new[] { highlight, highlight.WithAlpha(0) },
                    new float[] { 0f, 1f },
                    SKShaderTileMode.Clamp)
            })
            {
                canvas.DrawRect(bounds, highlightPaint);
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
