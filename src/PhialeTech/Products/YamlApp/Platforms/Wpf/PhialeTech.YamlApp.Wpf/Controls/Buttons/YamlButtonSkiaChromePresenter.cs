using System;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace PhialeTech.YamlApp.Wpf.Controls.Buttons
{
    public sealed class YamlButtonSkiaChromePresenter : SKElement
    {
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HoverBackgroundBrushProperty =
            DependencyProperty.Register(nameof(HoverBackgroundBrush), typeof(Brush), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HoverBorderBrushProperty =
            DependencyProperty.Register(nameof(HoverBorderBrush), typeof(Brush), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty FocusBrushProperty =
            DependencyProperty.Register(nameof(FocusBrush), typeof(Brush), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register(nameof(IsHovered), typeof(bool), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.Register(nameof(IsPressed), typeof(bool), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty HasKeyboardFocusProperty =
            DependencyProperty.Register(nameof(HasKeyboardFocus), typeof(bool), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty IsEnabledStateProperty =
            DependencyProperty.Register(nameof(IsEnabledState), typeof(bool), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(YamlButtonSkiaChromePresenter), new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidatePropertyChanged));

        public YamlButtonSkiaChromePresenter()
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
            ((YamlButtonSkiaChromePresenter)d).InvalidateVisual();
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
            var border = IsPressed || IsHovered
                ? ToSkColor(HoverBorderBrush)
                : ToSkColor(BorderBrush);
            var focus = ToSkColor(FocusBrush);

            if (!IsEnabledState)
            {
                fill = fill.WithAlpha((byte)Math.Min((int)fill.Alpha, 180));
                border = border.WithAlpha((byte)Math.Min((int)border.Alpha, 180));
                focus = focus.WithAlpha((byte)Math.Min((int)focus.Alpha, 180));
            }

            using (var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = fill })
            using (var borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, Color = border })
            {
                var roundRect = new SKRoundRect(bounds, radius, radius);
                canvas.DrawRoundRect(roundRect, fillPaint);

                var inset = borderPaint.StrokeWidth / 2f;
                var borderRect = new SKRect(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
                canvas.DrawRoundRect(new SKRoundRect(borderRect, Math.Max(0f, radius - inset), Math.Max(0f, radius - inset)), borderPaint);

                if (HasKeyboardFocus)
                {
                    DrawFocusRing(canvas, bounds, radius, fill, focus);
                }
            }
        }

        private static void DrawFocusRing(SKCanvas canvas, SKRect bounds, float radius, SKColor fill, SKColor focus)
        {
            const float outerStrokeWidth = 2f;
            const float innerStrokeWidth = 1.25f;

            using (var focusPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = outerStrokeWidth,
                Color = focus
            })
            {
                var outerInset = focusPaint.StrokeWidth / 2f;
                var outerRect = new SKRect(bounds.Left + outerInset, bounds.Top + outerInset, bounds.Right - outerInset, bounds.Bottom - outerInset);
                canvas.DrawRoundRect(
                    new SKRoundRect(outerRect, Math.Max(0f, radius - outerInset), Math.Max(0f, radius - outerInset)),
                    focusPaint);
            }

            if (!NeedsInnerContrastRing(fill, focus))
            {
                return;
            }

            var contrast = RelativeLuminance(fill) < 0.55f
                ? new SKColor(255, 255, 255, 230)
                : new SKColor(15, 23, 42, 180);

            using (var contrastPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = innerStrokeWidth,
                Color = contrast
            })
            {
                var innerInset = 3.5f;
                var innerRect = new SKRect(bounds.Left + innerInset, bounds.Top + innerInset, bounds.Right - innerInset, bounds.Bottom - innerInset);
                canvas.DrawRoundRect(
                    new SKRoundRect(innerRect, Math.Max(0f, radius - innerInset), Math.Max(0f, radius - innerInset)),
                    contrastPaint);
            }
        }

        private static bool NeedsInnerContrastRing(SKColor fill, SKColor focus)
        {
            if (fill.Alpha == 0)
            {
                return false;
            }

            var distance =
                Math.Abs(fill.Red - focus.Red) +
                Math.Abs(fill.Green - focus.Green) +
                Math.Abs(fill.Blue - focus.Blue);

            return distance < 90 || RelativeLuminance(fill) < 0.42f;
        }

        private static float RelativeLuminance(SKColor color)
        {
            return ((0.2126f * color.Red) + (0.7152f * color.Green) + (0.0722f * color.Blue)) / 255f;
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
