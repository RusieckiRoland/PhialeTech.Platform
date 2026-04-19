using System;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace PhialeTech.ComponentHost.Wpf.Controls
{
    public sealed class PhialeModalChromeSkiaPresenter : SKElement
    {
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(PhialeModalChromeSkiaPresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(PhialeModalChromeSkiaPresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty ShadowBrushProperty =
            DependencyProperty.Register(nameof(ShadowBrush), typeof(Brush), typeof(PhialeModalChromeSkiaPresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(PhialeModalChromeSkiaPresenter), new FrameworkPropertyMetadata(18d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public PhialeModalChromeSkiaPresenter()
        {
            PaintSurface += OnPaintSurface;
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

        public Brush ShadowBrush
        {
            get => (Brush)GetValue(ShadowBrushProperty);
            set => SetValue(ShadowBrushProperty, value);
        }

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        private static void OnInvalidatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PhialeModalChromeSkiaPresenter)d).InvalidateVisual();
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
            var background = ToSkColor(BackgroundBrush);
            var border = ToSkColor(BorderBrush);
            var shadow = ToSkColor(ShadowBrush);

            using (var shadowPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = shadow,
                ImageFilter = SKImageFilter.CreateDropShadow(0f, 10f, 18f, 18f, shadow)
            })
            {
                var shadowRect = new SKRect(bounds.Left + 10f, bounds.Top + 10f, bounds.Right - 10f, bounds.Bottom - 10f);
                canvas.DrawRoundRect(new SKRoundRect(shadowRect, Math.Max(0f, radius - 10f), Math.Max(0f, radius - 10f)), shadowPaint);
            }

            using (var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = background })
            using (var borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = border })
            {
                var inset = 8f;
                var rect = new SKRect(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
                var roundRect = new SKRoundRect(rect, Math.Max(0f, radius - inset), Math.Max(0f, radius - inset));
                canvas.DrawRoundRect(roundRect, fillPaint);

                var borderInset = inset + (borderPaint.StrokeWidth / 2f);
                var borderRect = new SKRect(bounds.Left + borderInset, bounds.Top + borderInset, bounds.Right - borderInset, bounds.Bottom - borderInset);
                canvas.DrawRoundRect(new SKRoundRect(borderRect, Math.Max(0f, radius - borderInset), Math.Max(0f, radius - borderInset)), borderPaint);
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
