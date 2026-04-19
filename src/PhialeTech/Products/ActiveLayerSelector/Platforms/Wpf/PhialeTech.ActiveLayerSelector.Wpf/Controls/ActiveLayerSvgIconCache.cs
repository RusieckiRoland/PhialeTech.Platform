using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;
using PhialeTech.ActiveLayerSelector;

namespace PhialeTech.ActiveLayerSelector.Wpf.Controls
{
    internal static class ActiveLayerSvgIconCache
    {
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new ConcurrentDictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";
        private const string MutedIconColor = "#8A95A3";

        public static ImageSource GetCapabilityImage(ActiveLayerSelectorCapabilityKind capabilityKind, bool isOn)
        {
            var stem = capabilityKind.ToString().ToLowerInvariant();
            var variant = isOn ? "on" : "off";
            var resourceName = typeof(ActiveLayerSvgIconCache).Assembly
                .GetManifestResourceNames()
                .First(name => name.EndsWith($"Assets.Icons.{stem}_{variant}_24.svg", StringComparison.OrdinalIgnoreCase));
            return Cache.GetOrAdd(resourceName, LoadImage);
        }

        private static ImageSource LoadImage(string resourceName)
        {
            using (var stream = typeof(ActiveLayerSvgIconCache).Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("SVG resource not found: " + resourceName)))
            {
                var document = XDocument.Parse(reader.ReadToEnd());
                var drawingGroup = new DrawingGroup();
                foreach (var element in document.Root.Elements())
                {
                    if (element.Name == SvgNamespace + "path")
                    {
                        var geometry = Geometry.Parse(element.Attribute("d")?.Value ?? string.Empty);
                        geometry.Freeze();
                        drawingGroup.Children.Add(new GeometryDrawing(ParseFill(element), CreatePen(element), geometry));
                    }
                    else if (element.Name == SvgNamespace + "circle")
                    {
                        var centerX = ParseDouble(element.Attribute("cx")?.Value);
                        var centerY = ParseDouble(element.Attribute("cy")?.Value);
                        var radius = ParseDouble(element.Attribute("r")?.Value);
                        var geometry = new EllipseGeometry(new System.Windows.Point(centerX, centerY), radius, radius);
                        geometry.Freeze();
                        drawingGroup.Children.Add(new GeometryDrawing(ParseFill(element), CreatePen(element), geometry));
                    }
                }

                drawingGroup.Freeze();
                var drawingImage = new DrawingImage(drawingGroup);
                drawingImage.Freeze();
                return drawingImage;
            }
        }

        private static Brush ParseFill(XElement element)
        {
            var fillValue = element.Attribute("fill")?.Value;
            if (string.IsNullOrWhiteSpace(fillValue) || string.Equals(fillValue, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var brush = (Brush)new BrushConverter().ConvertFromString(NormalizeMutedColor(fillValue));
            brush.Freeze();
            return brush;
        }

        private static Pen CreatePen(XElement element)
        {
            var strokeValue = element.Attribute("stroke")?.Value;
            if (string.IsNullOrWhiteSpace(strokeValue) || string.Equals(strokeValue, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var brush = (Brush)new BrushConverter().ConvertFromString(NormalizeMutedColor(strokeValue));
            brush.Freeze();
            var pen = new Pen(brush, ParseDouble(element.Attribute("stroke-width")?.Value));
            var lineCap = element.Attribute("stroke-linecap")?.Value;
            if (string.Equals(lineCap, "round", StringComparison.OrdinalIgnoreCase))
            {
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
            }

            var lineJoin = element.Attribute("stroke-linejoin")?.Value;
            if (string.Equals(lineJoin, "round", StringComparison.OrdinalIgnoreCase))
            {
                pen.LineJoin = PenLineJoin.Round;
            }
            else if (string.Equals(lineJoin, "bevel", StringComparison.OrdinalIgnoreCase))
            {
                pen.LineJoin = PenLineJoin.Bevel;
            }

            pen.Freeze();
            return pen;
        }

        private static double ParseDouble(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? 0d : double.Parse(value, CultureInfo.InvariantCulture);
        }

        private static string NormalizeMutedColor(string color)
        {
            return string.Equals(color, "#B8C1CC", StringComparison.OrdinalIgnoreCase)
                ? MutedIconColor
                : color;
        }
    }
}
