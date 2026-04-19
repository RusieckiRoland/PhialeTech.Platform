using System;
using System.Collections.Generic;
using System.Globalization;
using PhialeGis.Library.Abstractions.Styling;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class SymbolCompiler
    {
        public CompiledSymbol Compile(SymbolDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var contentHash = ComputeContentHash(definition);
            var primitives = new List<CompiledSymbolPrimitive>(definition.Primitives.Count);
            var hasBounds = false;
            var bounds = SKRect.Empty;

            for (int i = 0; i < definition.Primitives.Count; i++)
            {
                var compiledPrimitive = CompilePrimitive(definition.Primitives[i], i);
                primitives.Add(compiledPrimitive);

                var primitiveBounds = InflateBounds(compiledPrimitive.GeometryPath.Bounds, compiledPrimitive.StrokeWidth);
                if (!hasBounds)
                {
                    bounds = primitiveBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds = Union(bounds, primitiveBounds);
                }
            }

            if (!hasBounds)
                bounds = new SKRect(0f, 0f, 1f, 1f);

            using (var recorder = new SKPictureRecorder())
            {
                var canvas = recorder.BeginRecording(bounds);
                DrawPrimitives(canvas, primitives);
                var picture = recorder.EndRecording();

                return new CompiledSymbol(
                    definition.Id,
                    contentHash,
                    definition.AnchorX,
                    definition.AnchorY,
                    definition.DefaultSize,
                    bounds,
                    picture,
                    primitives.ToArray());
            }
        }

        public int ComputeContentHash(SymbolDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            unchecked
            {
                var hash = 17;
                hash = Add(hash, definition.Id);
                hash = Add(hash, definition.Name);
                hash = Add(hash, definition.AnchorX);
                hash = Add(hash, definition.AnchorY);
                hash = Add(hash, definition.DefaultSize);
                hash = Add(hash, definition.Primitives.Count);

                for (int i = 0; i < definition.Primitives.Count; i++)
                {
                    var primitive = definition.Primitives[i];
                    hash = Add(hash, (int)primitive.Kind);
                    hash = Add(hash, primitive.StrokeColorArgb);
                    hash = Add(hash, primitive.FillColorArgb);
                    hash = Add(hash, primitive.StrokeWidth);
                    hash = Add(hash, primitive.Coordinates.Length);

                    for (int c = 0; c < primitive.Coordinates.Length; c++)
                        hash = Add(hash, primitive.Coordinates[c]);
                }

                return hash;
            }
        }

        private static CompiledSymbolPrimitive CompilePrimitive(StylePrimitive primitive, int index)
        {
            if (primitive == null)
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Symbol primitive at index {0} cannot be null.", index),
                    nameof(primitive));

            var path = new SKPath();

            switch (primitive.Kind)
            {
                case SymbolPrimitiveKind.Polyline:
                    BuildPolylinePath(path, primitive.Coordinates, index);
                    break;

                case SymbolPrimitiveKind.Polygon:
                    BuildPolygonPath(path, primitive.Coordinates, index);
                    break;

                case SymbolPrimitiveKind.Circle:
                    BuildCirclePath(path, primitive.Coordinates, index);
                    break;

                default:
                    path.Dispose();
                    throw new ArgumentOutOfRangeException(nameof(primitive.Kind), primitive.Kind, "Unsupported symbol primitive kind.");
            }

            return new CompiledSymbolPrimitive(
                primitive.Kind,
                path,
                primitive.StrokeColorArgb,
                primitive.FillColorArgb,
                primitive.StrokeWidth);
        }

        private static void DrawPrimitives(SKCanvas canvas, IReadOnlyList<CompiledSymbolPrimitive> primitives)
        {
            for (int i = 0; i < primitives.Count; i++)
            {
                var primitive = primitives[i];

                if (HasVisibleFill(primitive))
                {
                    using (var fillPaint = new SKPaint
                    {
                        IsStroke = false,
                        IsAntialias = true,
                        Color = FromArgb(primitive.FillColorArgb)
                    })
                    {
                        canvas.DrawPath(primitive.GeometryPath, fillPaint);
                    }
                }

                if (HasVisibleStroke(primitive))
                {
                    using (var strokePaint = new SKPaint
                    {
                        IsStroke = true,
                        IsAntialias = true,
                        Color = FromArgb(primitive.StrokeColorArgb),
                        StrokeWidth = (float)primitive.StrokeWidth,
                        StrokeCap = SKStrokeCap.Round,
                        StrokeJoin = SKStrokeJoin.Round
                    })
                    {
                        canvas.DrawPath(primitive.GeometryPath, strokePaint);
                    }
                }
            }
        }

        private static bool HasVisibleFill(CompiledSymbolPrimitive primitive)
        {
            return ((uint)primitive.FillColorArgb >> 24) != 0;
        }

        private static bool HasVisibleStroke(CompiledSymbolPrimitive primitive)
        {
            return primitive.StrokeWidth > 0d && ((uint)primitive.StrokeColorArgb >> 24) != 0;
        }

        private static void BuildPolylinePath(SKPath path, double[] coordinates, int primitiveIndex)
        {
            ValidateEvenCoordinateArray(coordinates, 4, primitiveIndex, SymbolPrimitiveKind.Polyline);

            path.MoveTo((float)coordinates[0], (float)coordinates[1]);
            for (int i = 2; i < coordinates.Length; i += 2)
                path.LineTo((float)coordinates[i], (float)coordinates[i + 1]);
        }

        private static void BuildPolygonPath(SKPath path, double[] coordinates, int primitiveIndex)
        {
            ValidateEvenCoordinateArray(coordinates, 6, primitiveIndex, SymbolPrimitiveKind.Polygon);

            path.MoveTo((float)coordinates[0], (float)coordinates[1]);
            for (int i = 2; i < coordinates.Length; i += 2)
                path.LineTo((float)coordinates[i], (float)coordinates[i + 1]);

            path.Close();
        }

        private static void BuildCirclePath(SKPath path, double[] coordinates, int primitiveIndex)
        {
            if (coordinates == null || coordinates.Length != 3)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Symbol primitive at index {0} of kind {1} must provide exactly centerX, centerY, radius.",
                        primitiveIndex,
                        SymbolPrimitiveKind.Circle),
                    nameof(coordinates));
            }

            var radius = (float)coordinates[2];
            if (radius <= 0f)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Circle primitive at index {0} must have a positive radius.", primitiveIndex),
                    nameof(coordinates));
            }

            path.AddCircle((float)coordinates[0], (float)coordinates[1], radius);
        }

        private static void ValidateEvenCoordinateArray(
            double[] coordinates,
            int minimumLength,
            int primitiveIndex,
            SymbolPrimitiveKind kind)
        {
            if (coordinates == null || coordinates.Length < minimumLength || (coordinates.Length % 2) != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Symbol primitive at index {0} of kind {1} must provide an even coordinate array of length >= {2}.",
                        primitiveIndex,
                        kind,
                        minimumLength),
                    nameof(coordinates));
            }
        }

        private static SKRect InflateBounds(SKRect bounds, double strokeWidth)
        {
            if (strokeWidth <= 0d)
                return bounds;

            var delta = (float)(strokeWidth / 2d);
            return new SKRect(bounds.Left - delta, bounds.Top - delta, bounds.Right + delta, bounds.Bottom + delta);
        }

        private static SKRect Union(SKRect left, SKRect right)
        {
            return new SKRect(
                Math.Min(left.Left, right.Left),
                Math.Min(left.Top, right.Top),
                Math.Max(left.Right, right.Right),
                Math.Max(left.Bottom, right.Bottom));
        }

        private static SKColor FromArgb(int argb)
        {
            var color = unchecked((uint)argb);
            var a = (byte)((color >> 24) & 0xFF);
            var r = (byte)((color >> 16) & 0xFF);
            var g = (byte)((color >> 8) & 0xFF);
            var b = (byte)(color & 0xFF);
            return new SKColor(r, g, b, a);
        }

        private static int Add(int current, string value)
        {
            return (current * 31) + StringComparerOrdinal.GetHashCode(value ?? string.Empty);
        }

        private static int Add(int current, int value)
        {
            return (current * 31) + value;
        }

        private static int Add(int current, double value)
        {
            return (current * 31) + value.GetHashCode();
        }

        private static readonly StringComparer StringComparerOrdinal = StringComparer.Ordinal;
    }
}
