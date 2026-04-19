using System;
using System.Windows;
using System.Windows.Media;
using PhialeTech.YamlApp.Core.Controls.TextBox;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace PhialeTech.YamlApp.Wpf.Controls.TextBox
{
    public sealed class YamlTextBoxSkiaChromePresenter : SKElement
    {
        public static readonly DependencyProperty ChromeStateProperty =
            DependencyProperty.Register(nameof(ChromeState), typeof(YamlTextBoxChromeState), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty SurfaceBrushProperty =
            DependencyProperty.Register(nameof(SurfaceBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty FocusBrushProperty =
            DependencyProperty.Register(nameof(FocusBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty DangerFillBrushProperty =
            DependencyProperty.Register(nameof(DangerFillBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.MistyRose, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty DangerBorderBrushProperty =
            DependencyProperty.Register(nameof(DangerBorderBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.IndianRed, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty CaptionBrushProperty =
            DependencyProperty.Register(nameof(CaptionBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty SupportBrushProperty =
            DependencyProperty.Register(nameof(SupportBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty DangerTextBrushProperty =
            DependencyProperty.Register(nameof(DangerTextBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.IndianRed, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty PlaceholderBrushProperty =
            DependencyProperty.Register(nameof(PlaceholderBrush), typeof(Brush), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(Brushes.SlateGray, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty CaptionFontFamilyProperty =
            DependencyProperty.Register(nameof(CaptionFontFamily), typeof(FontFamily), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty SupportFontFamilyProperty =
            DependencyProperty.Register(nameof(SupportFontFamily), typeof(FontFamily), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty EditorFontFamilyProperty =
            DependencyProperty.Register(nameof(EditorFontFamily), typeof(FontFamily), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty CaptionFontSizeProperty =
            DependencyProperty.Register(nameof(CaptionFontSize), typeof(double), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty SupportFontSizeProperty =
            DependencyProperty.Register(nameof(SupportFontSize), typeof(double), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty EditorFontSizeProperty =
            DependencyProperty.Register(nameof(EditorFontSize), typeof(double), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(13d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty CardCornerRadiusProperty =
            DependencyProperty.Register(nameof(CardCornerRadius), typeof(double), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public static readonly DependencyProperty InputCornerRadiusProperty =
            DependencyProperty.Register(nameof(InputCornerRadius), typeof(double), typeof(YamlTextBoxSkiaChromePresenter), new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsRender, OnInvalidateVisualPropertyChanged));

        public YamlTextBoxSkiaChromePresenter()
        {
            IgnorePixelScaling = false;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            PaintSurface += OnPaintSurface;
        }

        public YamlTextBoxChromeState ChromeState
        {
            get => (YamlTextBoxChromeState)GetValue(ChromeStateProperty);
            set => SetValue(ChromeStateProperty, value);
        }

        public Brush SurfaceBrush
        {
            get => (Brush)GetValue(SurfaceBrushProperty);
            set => SetValue(SurfaceBrushProperty, value);
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

        public Brush FocusBrush
        {
            get => (Brush)GetValue(FocusBrushProperty);
            set => SetValue(FocusBrushProperty, value);
        }

        public Brush DangerFillBrush
        {
            get => (Brush)GetValue(DangerFillBrushProperty);
            set => SetValue(DangerFillBrushProperty, value);
        }

        public Brush DangerBorderBrush
        {
            get => (Brush)GetValue(DangerBorderBrushProperty);
            set => SetValue(DangerBorderBrushProperty, value);
        }

        public Brush CaptionBrush
        {
            get => (Brush)GetValue(CaptionBrushProperty);
            set => SetValue(CaptionBrushProperty, value);
        }

        public Brush SupportBrush
        {
            get => (Brush)GetValue(SupportBrushProperty);
            set => SetValue(SupportBrushProperty, value);
        }

        public Brush DangerTextBrush
        {
            get => (Brush)GetValue(DangerTextBrushProperty);
            set => SetValue(DangerTextBrushProperty, value);
        }

        public Brush PlaceholderBrush
        {
            get => (Brush)GetValue(PlaceholderBrushProperty);
            set => SetValue(PlaceholderBrushProperty, value);
        }

        public FontFamily CaptionFontFamily
        {
            get => (FontFamily)GetValue(CaptionFontFamilyProperty);
            set => SetValue(CaptionFontFamilyProperty, value);
        }

        public FontFamily SupportFontFamily
        {
            get => (FontFamily)GetValue(SupportFontFamilyProperty);
            set => SetValue(SupportFontFamilyProperty, value);
        }

        public FontFamily EditorFontFamily
        {
            get => (FontFamily)GetValue(EditorFontFamilyProperty);
            set => SetValue(EditorFontFamilyProperty, value);
        }

        public double CaptionFontSize
        {
            get => (double)GetValue(CaptionFontSizeProperty);
            set => SetValue(CaptionFontSizeProperty, value);
        }

        public double SupportFontSize
        {
            get => (double)GetValue(SupportFontSizeProperty);
            set => SetValue(SupportFontSizeProperty, value);
        }

        public double EditorFontSize
        {
            get => (double)GetValue(EditorFontSizeProperty);
            set => SetValue(EditorFontSizeProperty, value);
        }

        public double CardCornerRadius
        {
            get => (double)GetValue(CardCornerRadiusProperty);
            set => SetValue(CardCornerRadiusProperty, value);
        }

        public double InputCornerRadius
        {
            get => (double)GetValue(InputCornerRadiusProperty);
            set => SetValue(InputCornerRadiusProperty, value);
        }

        private static void OnInvalidateVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YamlTextBoxSkiaChromePresenter)d).InvalidateVisual();
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            var state = ChromeState;
            if (state == null)
            {
                return;
            }

            var render = RenderContext.Create(
                e.Info,
                ActualWidth,
                ActualHeight,
                CardCornerRadius,
                InputCornerRadius);

            if (render.IsEmpty)
            {
                return;
            }

            canvas.Clear(SKColors.Transparent);
            canvas.ClipRect(render.BoundsPx, SKClipOperation.Intersect, true);

            var disabledAlpha = state.IsEnabled ? 255 : 150;
            var shellFillColor = ResolveShellFillColor(state, disabledAlpha);
            var borderColor = ResolveBorderColor(state, disabledAlpha);
            var captionColor = StrengthenColor(ApplyAlpha(CaptionBrush, disabledAlpha), state.IsEnabled ? 0.18f : 0.1f);
            var supportColor = StrengthenColor(ApplyAlpha(state.HasError ? DangerTextBrush : SupportBrush, disabledAlpha), state.HasError ? 0.14f : 0f);
            var placeholderColor = ApplyAlpha(PlaceholderBrush, disabledAlpha);

            if (state.UsesFramedChrome)
            {
                DrawFramedChrome(canvas, render, state, shellFillColor, borderColor, captionColor, supportColor, placeholderColor);
            }
            else
            {
                DrawInlineChrome(canvas, render, state, borderColor, captionColor, supportColor, placeholderColor);
            }
        }

        private void DrawFramedChrome(SKCanvas canvas, RenderContext render, YamlTextBoxChromeState state, SKColor shellFillColor, SKColor borderColor, SKColor captionColor, SKColor supportColor, SKColor placeholderColor)
        {
            var rect = render.BoundsPx;
            var shellBorderColor = state.HasError
                ? ApplyAlpha(DangerBorderBrush, state.IsEnabled ? 255 : 170)
                : ApplyAlpha(BorderBrush, 160);
            var headerLeft = render.ToPxX(12d);
            var editorLeft = render.ToPxX(state.LayoutMetrics.EditorLeft);
            var editorTextInset = render.MeasureX(GetEditorTextInsetDip(state));
            var editorTextBaseline = CalculateCenteredTextBaseline(render, state, EditorFontFamily, (float)EditorFontSize, SKFontStyleWeight.Normal);
            var titleBaseline = state.CaptionPlacement == Abstractions.Enums.CaptionPlacement.Left
                ? editorTextBaseline
                : CalculateBaselineForBottomGap(render, state.LayoutMetrics.EditorTop - 2d, CaptionFontFamily, (float)CaptionFontSize, SKFontStyleWeight.SemiBold);
            var supportLeft = state.CaptionPlacement == Abstractions.Enums.CaptionPlacement.Left ? editorLeft : Math.Max(render.ToPxX(12d), editorLeft);
            var supportBaseline = CalculateBaselineForTopGap(render, GetEditorBottomDip(render, state) + 2d, SupportFontFamily, (float)SupportFontSize, state.HasError ? SKFontStyleWeight.Medium : SKFontStyleWeight.Medium);

            DrawRoundedFill(canvas, rect, render.CardRadiusPx, shellFillColor);
            DrawRoundedInsideBorder(canvas, rect, render.CardRadiusPx, shellBorderColor, 1f, 1f, 1f, 1f);

            DrawText(canvas, render, state.Caption, headerLeft, titleBaseline, CaptionFontFamily, (float)CaptionFontSize, captionColor, SKFontStyleWeight.SemiBold);
            DrawEditorBorder(canvas, render, state, borderColor);

            if (!state.HasFocus && string.IsNullOrWhiteSpace(state.Text) && !string.IsNullOrWhiteSpace(state.Placeholder))
            {
                DrawTextClippedToRect(
                    canvas,
                    render,
                    state.Placeholder,
                    editorLeft + editorTextInset,
                    editorTextBaseline,
                    EditorFontFamily,
                    (float)EditorFontSize,
                    placeholderColor,
                    SKFontStyleWeight.Normal,
                    new SKRect(
                        editorLeft + editorTextInset,
                        render.ToPxY(state.LayoutMetrics.EditorTop),
                        render.WidthPx - render.MeasureX(state.LayoutMetrics.EditorRight) - editorTextInset,
                        render.HeightPx - render.MeasureY(state.LayoutMetrics.EditorBottom)));
            }

            if (!string.IsNullOrWhiteSpace(state.SupportText))
            {
                DrawText(
                    canvas,
                    render,
                    state.SupportText,
                    supportLeft,
                    supportBaseline,
                    SupportFontFamily,
                    (float)SupportFontSize,
                    supportColor,
                    state.HasError ? SKFontStyleWeight.Medium : SKFontStyleWeight.Medium);
            }
        }

        private void DrawInlineChrome(SKCanvas canvas, RenderContext render, YamlTextBoxChromeState state, SKColor borderColor, SKColor captionColor, SKColor supportColor, SKColor placeholderColor)
        {
            var captionLeft = 0f;
            var editorLeft = render.ToPxX(state.LayoutMetrics.EditorLeft);
            var editorTextInset = render.MeasureX(GetEditorTextInsetDip(state));
            var editorTextBaseline = CalculateCenteredTextBaseline(render, state, EditorFontFamily, (float)EditorFontSize, SKFontStyleWeight.Normal);
            var captionBaseline = state.CaptionPlacement == Abstractions.Enums.CaptionPlacement.Left
                ? editorTextBaseline
                : CalculateBaselineForBottomGap(render, state.LayoutMetrics.EditorTop - 2d, CaptionFontFamily, Math.Max(11f, (float)CaptionFontSize - 1f), SKFontStyleWeight.SemiBold);
            var supportLeft = state.CaptionPlacement == Abstractions.Enums.CaptionPlacement.Left ? editorLeft : 0f;

            DrawText(canvas, render, state.Caption, captionLeft, captionBaseline, CaptionFontFamily, Math.Max(11f, (float)CaptionFontSize - 1f), captionColor, SKFontStyleWeight.SemiBold);
            DrawEditorBorder(canvas, render, state, borderColor);

            if (!state.HasFocus && string.IsNullOrWhiteSpace(state.Text) && !string.IsNullOrWhiteSpace(state.Placeholder))
            {
                DrawTextClippedToRect(
                    canvas,
                    render,
                    state.Placeholder,
                    editorLeft + editorTextInset,
                    editorTextBaseline,
                    EditorFontFamily,
                    (float)EditorFontSize,
                    placeholderColor,
                    SKFontStyleWeight.Normal,
                    new SKRect(
                        editorLeft + editorTextInset,
                        render.ToPxY(state.LayoutMetrics.EditorTop),
                        render.WidthPx - render.MeasureX(state.LayoutMetrics.EditorRight) - editorTextInset,
                        render.HeightPx - render.MeasureY(state.LayoutMetrics.EditorBottom)));
            }

            if (!string.IsNullOrWhiteSpace(state.SupportText))
            {
                DrawText(
                    canvas,
                    render,
                    state.SupportText,
                    supportLeft,
                    CalculateBaselineForTopGap(render, GetEditorBottomDip(render, state) + 2d, SupportFontFamily, Math.Max(11f, (float)SupportFontSize), state.HasError ? SKFontStyleWeight.Medium : SKFontStyleWeight.Medium),
                    SupportFontFamily,
                    Math.Max(11f, (float)SupportFontSize),
                    supportColor,
                    state.HasError ? SKFontStyleWeight.Medium : SKFontStyleWeight.Medium);
            }
        }

        private void DrawEditorBorder(SKCanvas canvas, RenderContext render, YamlTextBoxChromeState state, SKColor borderColor)
        {
            var left = render.ToPxX(state.LayoutMetrics.EditorLeft);
            var top = render.ToPxY(state.LayoutMetrics.EditorTop);
            var right = render.WidthPx - render.MeasureX(state.LayoutMetrics.EditorRight);
            var bottom = render.HeightPx - render.MeasureY(state.LayoutMetrics.EditorBottom);
            var radius = state.UsesFramedChrome ? render.InputRadiusPx : render.ToRadiusPx(5d);
            var fillColor = ApplyAlpha(SurfaceBrush, state.IsEnabled ? 255 : 185);
            var rect = new SKRect(left, top, right, bottom);

            DrawRoundedFill(canvas, rect, radius, fillColor);

            if (state.HasFocus)
            {
                DrawRoundedInsideBorder(canvas, rect, radius, borderColor, 1f, 1f, 2f, 1f);
                return;
            }

            DrawRoundedInsideBorder(canvas, rect, radius, borderColor, 1f, 1f, 1f, 1f);
        }

        private void DrawText(SKCanvas canvas, RenderContext render, string text, float xPx, float baselinePx, FontFamily fontFamily, float fontSizeDip, SKColor color, SKFontStyleWeight weight)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var snappedX = render.Snap(xPx);
            var snappedBaseline = render.Snap(baselinePx);
            var snappedFontSize = render.ToFontPx(fontSizeDip);

            using (var paint = new SKPaint
            {
                IsAntialias = true,
                Color = color,
            })
            using (var font = new SKFont(CreateTypeface(fontFamily, weight), snappedFontSize)
            {
                BaselineSnap = true,
                Edging = SKFontEdging.SubpixelAntialias,
                Hinting = SKFontHinting.Full,
                ForceAutoHinting = true,
                Subpixel = true,
                LinearMetrics = false,
                Embolden = false,
            })
            {
                canvas.DrawText(text, snappedX, snappedBaseline, SKTextAlign.Left, font, paint);
            }
        }

        private void DrawTextClippedToRect(SKCanvas canvas, RenderContext render, string text, float xPx, float baselinePx, FontFamily fontFamily, float fontSizeDip, SKColor color, SKFontStyleWeight weight, SKRect clipRect)
        {
            if (string.IsNullOrWhiteSpace(text) || clipRect.Width <= 0f || clipRect.Height <= 0f)
            {
                return;
            }

            var restore = canvas.Save();
            canvas.ClipRect(clipRect, SKClipOperation.Intersect, true);
            DrawText(canvas, render, text, xPx, baselinePx, fontFamily, fontSizeDip, color, weight);
            canvas.RestoreToCount(restore);
        }

        private float CalculateBaselineForBottomGap(RenderContext render, double desiredBottomDip, FontFamily fontFamily, float fontSizeDip, SKFontStyleWeight weight)
        {
            var snappedFontSize = render.ToFontPx(fontSizeDip);
            using (var font = new SKFont(CreateTypeface(fontFamily, weight), snappedFontSize))
            {
                var metrics = font.Metrics;
                var desiredBottomPx = render.ToPxY(desiredBottomDip);
                return render.Snap(desiredBottomPx - metrics.Descent);
            }
        }

        private float CalculateBaselineForTopGap(RenderContext render, double desiredTopDip, FontFamily fontFamily, float fontSizeDip, SKFontStyleWeight weight)
        {
            var snappedFontSize = render.ToFontPx(fontSizeDip);
            using (var font = new SKFont(CreateTypeface(fontFamily, weight), snappedFontSize))
            {
                var metrics = font.Metrics;
                var desiredTopPx = render.ToPxY(desiredTopDip);
                return render.Snap(desiredTopPx - metrics.Ascent);
            }
        }

        private float CalculateCenteredTextBaseline(RenderContext render, YamlTextBoxChromeState state, FontFamily fontFamily, float fontSizeDip, SKFontStyleWeight weight)
        {
            var snappedFontSize = render.ToFontPx(fontSizeDip);
            using (var font = new SKFont(CreateTypeface(fontFamily, weight), snappedFontSize))
            {
                var metrics = font.Metrics;
                var editorTopDip = state.LayoutMetrics.EditorTop;
                var editorBottomDip = GetEditorBottomDip(render, state);
                var editorHeightDip = Math.Max(0d, editorBottomDip - editorTopDip);
                var textHeightDip = (metrics.Descent - metrics.Ascent) / render.ScaleY;
                var ascentDip = metrics.Ascent / render.ScaleY;
                var baselineDip = editorTopDip + ((editorHeightDip - textHeightDip) / 2d) - ascentDip;
                return render.ToPxY(baselineDip);
            }
        }

        private static double GetEditorTextInsetDip(YamlTextBoxChromeState state)
        {
            var density = state?.DensityMode ?? Abstractions.Enums.DensityMode.Normal;
            if (state != null && state.UsesInlineChrome)
            {
                switch (density)
                {
                    case Abstractions.Enums.DensityMode.Compact:
                        return 6d;
                    case Abstractions.Enums.DensityMode.Comfortable:
                        return 8d;
                    default:
                        return 7d;
                }
            }

            switch (density)
            {
                case Abstractions.Enums.DensityMode.Compact:
                    return 8d;
                case Abstractions.Enums.DensityMode.Comfortable:
                    return 11d;
                default:
                    return 9d;
            }
        }

        private static double GetEditorBottomDip(RenderContext render, YamlTextBoxChromeState state)
        {
            return render.HeightDip - state.LayoutMetrics.EditorBottom;
        }

        private void DrawRoundedFill(SKCanvas canvas, SKRect rect, float radiusPx, SKColor color)
        {
            using (var fill = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = color,
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(rect, radiusPx, radiusPx), fill);
            }
        }

        private void DrawRoundedInsideBorder(SKCanvas canvas, SKRect rect, float radiusPx, SKColor color, float leftThicknessPx, float topThicknessPx, float bottomThicknessPx, float rightThicknessPx)
        {
            if (rect.Width <= 0f || rect.Height <= 0f)
            {
                return;
            }

            var innerRect = new SKRect(
                rect.Left + leftThicknessPx,
                rect.Top + topThicknessPx,
                rect.Right - rightThicknessPx,
                rect.Bottom - bottomThicknessPx);

            if (innerRect.Width <= 0f || innerRect.Height <= 0f)
            {
                return;
            }

            var innerRadius = Math.Max(0f, radiusPx - Math.Max(Math.Max(leftThicknessPx, rightThicknessPx), Math.Max(topThicknessPx, bottomThicknessPx)));

            using (var border = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = color,
            })
            using (var ringPath = new SKPath { FillType = SKPathFillType.EvenOdd })
            {
                ringPath.AddRoundRect(new SKRoundRect(rect, radiusPx, radiusPx));
                ringPath.AddRoundRect(new SKRoundRect(innerRect, innerRadius, innerRadius));
                canvas.DrawPath(ringPath, border);
            }
        }

        private static SKTypeface CreateTypeface(FontFamily fontFamily, SKFontStyleWeight weight)
        {
            return SKTypeface.FromFamilyName(
                fontFamily?.Source,
                weight,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
        }

        private SKColor ResolveBorderColor(YamlTextBoxChromeState state, int alpha)
        {
            if (state.HasFocus)
            {
                return ApplyAlpha(FocusBrush, alpha);
            }

            if (state.HasError)
            {
                return ApplyAlpha(DangerBorderBrush, alpha);
            }

            if (state.InteractionMode == Abstractions.Enums.InteractionMode.Touch && state.HasPressed)
            {
                return ApplyAlpha(FocusBrush, Math.Min(255, alpha + 30));
            }

            if (state.InteractionMode == Abstractions.Enums.InteractionMode.Touch)
            {
                return ApplyAlpha(BorderBrush, alpha);
            }

            if (state.HasHover)
            {
                return ApplyAlpha(FocusBrush, alpha);
            }

            return ApplyAlpha(BorderBrush, alpha);
        }

        private SKColor ResolveShellFillColor(YamlTextBoxChromeState state, int alpha)
        {
            if (state.InteractionMode == Abstractions.Enums.InteractionMode.Touch && state.HasPressed && !state.HasError)
            {
                return Blend(ApplyAlpha(BackgroundBrush, alpha), ApplyAlpha(FocusBrush, alpha), 0.12f);
            }

            if (!state.HasError)
            {
                return ApplyAlpha(BackgroundBrush, alpha);
            }

            return ApplyAlpha(DangerFillBrush, alpha);
        }

        private static SKColor ApplyAlpha(Brush brush, int alpha)
        {
            var color = ToSkColor(brush);
            return color.WithAlpha((byte)Math.Max(0, Math.Min(255, alpha)));
        }

        private static SKColor Blend(SKColor first, SKColor second, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            var inverse = 1f - amount;
            return new SKColor(
                (byte)Math.Round((first.Red * inverse) + (second.Red * amount)),
                (byte)Math.Round((first.Green * inverse) + (second.Green * amount)),
                (byte)Math.Round((first.Blue * inverse) + (second.Blue * amount)),
                (byte)Math.Round((first.Alpha * inverse) + (second.Alpha * amount)));
        }

        private static SKColor StrengthenColor(SKColor color, float amount)
        {
            if (amount <= 0f)
            {
                return color;
            }

            var luminance = ((0.2126f * color.Red) + (0.7152f * color.Green) + (0.0722f * color.Blue)) / 255f;
            var target = luminance >= 0.56f ? SKColors.White : SKColors.Black;
            var strengthened = Blend(color, target, amount);
            return strengthened.WithAlpha(color.Alpha);
        }

        private static SKColor ToSkColor(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                return new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A);
            }

            return SKColors.Transparent;
        }

        private readonly struct RenderContext
        {
            private RenderContext(float widthPx, float heightPx, float scaleX, float scaleY, float cardRadiusPx, float inputRadiusPx)
            {
                WidthPx = widthPx;
                HeightPx = heightPx;
                ScaleX = scaleX;
                ScaleY = scaleY;
                CardRadiusPx = cardRadiusPx;
                InputRadiusPx = inputRadiusPx;
            }

            public float WidthPx { get; }

            public float HeightPx { get; }

            public double HeightDip => HeightPx / ScaleY;

            public float ScaleX { get; }

            public float ScaleY { get; }

            public float CardRadiusPx { get; }

            public float InputRadiusPx { get; }

            public bool IsEmpty => WidthPx <= 0f || HeightPx <= 0f || ScaleX <= 0f || ScaleY <= 0f;

            public SKRect BoundsPx => new SKRect(0f, 0f, WidthPx, HeightPx);

            public static RenderContext Create(SKImageInfo info, double actualWidthDip, double actualHeightDip, double cardRadiusDip, double inputRadiusDip)
            {
                var widthPx = Math.Max(0, info.Width);
                var heightPx = Math.Max(0, info.Height);
                var safeWidthDip = Math.Max(0.0001d, actualWidthDip);
                var safeHeightDip = Math.Max(0.0001d, actualHeightDip);
                var scaleX = (float)(widthPx / safeWidthDip);
                var scaleY = (float)(heightPx / safeHeightDip);

                return new RenderContext(
                    widthPx,
                    heightPx,
                    scaleX,
                    scaleY,
                    Math.Max(1f, (float)Math.Round(cardRadiusDip * Math.Min(scaleX, scaleY))),
                    Math.Max(1f, (float)Math.Round(inputRadiusDip * Math.Min(scaleX, scaleY))));
            }

            public float ToPxX(double dip)
            {
                return Snap((float)(dip * ScaleX));
            }

            public float ToPxY(double dip)
            {
                return Snap((float)(dip * ScaleY));
            }

            public float MeasureX(double dip)
            {
                return Math.Max(0f, Snap((float)(dip * ScaleX)));
            }

            public float MeasureY(double dip)
            {
                return Math.Max(0f, Snap((float)(dip * ScaleY)));
            }

            public float ToRadiusPx(double dip)
            {
                return Math.Max(0f, Snap((float)(dip * Math.Min(ScaleX, ScaleY))));
            }

            public float ToFontPx(double dip)
            {
                return Math.Max(1f, Snap((float)(dip * Math.Min(ScaleX, ScaleY))));
            }

            public float Snap(float value)
            {
                return (float)Math.Round(value);
            }
        }
    }
}
