using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PhialeTech.ActiveLayerSelector.Wpf.Controls
{
    public sealed class ActiveLayerGeometryGlyph : FrameworkElement
    {
        public static readonly DependencyProperty GeometryTypeProperty = DependencyProperty.Register(
            nameof(GeometryType),
            typeof(string),
            typeof(ActiveLayerGeometryGlyph),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsNightModeProperty = DependencyProperty.Register(
            nameof(IsNightMode),
            typeof(bool),
            typeof(ActiveLayerGeometryGlyph),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public string GeometryType
        {
            get => (string)GetValue(GeometryTypeProperty);
            set => SetValue(GeometryTypeProperty, value);
        }

        public bool IsNightMode
        {
            get => (bool)GetValue(IsNightModeProperty);
            set => SetValue(IsNightModeProperty, value);
        }

        public ActiveLayerGeometryGlyph()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var bounds = new Rect(0.5, 0.5, Math.Max(0, ActualWidth - 1), Math.Max(0, ActualHeight - 1));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var fillBrush = (Brush)new BrushConverter().ConvertFromString(IsNightMode ? "#2D3544" : "#F5F7FB");
            var strokeBrush = (Brush)new BrushConverter().ConvertFromString(IsNightMode ? "#95C3FF" : "#0B4F8A");
            var framePen = new Pen((Brush)new BrushConverter().ConvertFromString(IsNightMode ? "#5A667D" : "#D8DDE6"), 1);
            var iconPen = new Pen(strokeBrush, 1);
            drawingContext.DrawRoundedRectangle(fillBrush, framePen, bounds, 6, 6);

            var inner = new Rect(bounds.Left + 5, bounds.Top + 5, Math.Max(0, bounds.Width - 10), Math.Max(0, bounds.Height - 10));
            var normalized = (GeometryType ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "point")
            {
                drawingContext.DrawEllipse(strokeBrush, null, new Point(inner.Left + inner.Width / 2, inner.Top + inner.Height / 2), 4, 4);
            }
            else if (normalized == "linestring" || normalized == "line")
            {
                drawingContext.DrawLine(iconPen, new Point(inner.Left, inner.Bottom - 2), new Point(inner.Right, inner.Top + 2));
            }
            else if (normalized == "polygon")
            {
                var geometry = Geometry.Parse("M 8,1 L 15,5 L 13,14 L 4,15 L 1,7 Z").Clone();
                var transforms = new TransformGroup();
                transforms.Children.Add(new ScaleTransform(inner.Width / 16d, inner.Height / 16d));
                transforms.Children.Add(new TranslateTransform(inner.Left, inner.Top));
                geometry.Transform = transforms;
                drawingContext.DrawGeometry(null, iconPen, geometry);
            }
            else if (normalized == "raster")
            {
                var cellWidth = inner.Width / 2d;
                var cellHeight = inner.Height / 2d;
                var rasterBrush = (Brush)new BrushConverter().ConvertFromString(IsNightMode ? "#4F7FB8" : "#7AA6D9");
                drawingContext.DrawRectangle(rasterBrush, iconPen, new Rect(inner.Left, inner.Top, cellWidth - 1, cellHeight - 1));
                drawingContext.DrawRectangle(fillBrush, iconPen, new Rect(inner.Left + cellWidth, inner.Top, cellWidth - 1, cellHeight - 1));
                drawingContext.DrawRectangle(fillBrush, iconPen, new Rect(inner.Left, inner.Top + cellHeight, cellWidth - 1, cellHeight - 1));
                drawingContext.DrawRectangle(rasterBrush, iconPen, new Rect(inner.Left + cellWidth, inner.Top + cellHeight, cellWidth - 1, cellHeight - 1));
            }
            else
            {
                var text = new FormattedText("?", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), 10, strokeBrush, 1.0);
                drawingContext.DrawText(text, new Point(inner.Left + (inner.Width - text.Width) / 2, inner.Top + (inner.Height - text.Height) / 2));
            }
        }
    }
}

