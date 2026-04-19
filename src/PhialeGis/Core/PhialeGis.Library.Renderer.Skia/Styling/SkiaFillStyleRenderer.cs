using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class SkiaFillStyleRenderer
    {
        public void FillPolygon(
            SKCanvas canvas,
            SkiaViewportProjector projector,
            IList<PhPoint> outer,
            IList<IList<PhPoint>> holes,
            FillStyleDefinition fillStyle,
            int backgroundColorArgb)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (projector == null)
                throw new ArgumentNullException(nameof(projector));

            if (fillStyle == null)
                throw new ArgumentNullException(nameof(fillStyle));

            if (outer == null || outer.Count == 0)
                return;

            using (var path = BuildProjectedPath(projector, outer, holes))
            {
                FillPath(canvas, path, fillStyle, backgroundColorArgb);
            }
        }

        public void FillPath(
            SKCanvas canvas,
            SKPath path,
            FillStyleDefinition fillStyle,
            int backgroundColorArgb)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (fillStyle == null)
                throw new ArgumentNullException(nameof(fillStyle));

            var bounds = path.Bounds;

            switch (fillStyle.Kind)
            {
                case FillStyleKind.Gradient:
                    using (var paint = new SKPaint
                    {
                        IsStroke = false,
                        IsAntialias = true,
                        Shader = CreateGradientShader(bounds, fillStyle, backgroundColorArgb)
                    })
                    {
                        canvas.DrawPath(path, paint);
                    }
                    break;

                case FillStyleKind.Hatch:
                    FillSolid(canvas, path, fillStyle.BackColorArgb != 0 ? fillStyle.BackColorArgb : backgroundColorArgb);
                    DrawHatch(canvas, path, bounds, fillStyle);
                    break;

                case FillStyleKind.PatternTile:
                    FillSolid(canvas, path, fillStyle.BackColorArgb != 0 ? fillStyle.BackColorArgb : backgroundColorArgb);
                    DrawPatternTile(canvas, path, bounds, fillStyle, backgroundColorArgb);
                    break;

                default:
                    FillSolid(canvas, path, fillStyle.ForeColorArgb);
                    break;
            }
        }

        private static SKPath BuildProjectedPath(
            SkiaViewportProjector projector,
            IList<PhPoint> outer,
            IList<IList<PhPoint>> holes)
        {
            var path = new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };

            AddRing(path, projector.ProjectPoints(outer));

            if (holes != null)
            {
                for (int i = 0; i < holes.Count; i++)
                {
                    var hole = holes[i];
                    if (hole == null || hole.Count == 0)
                        continue;

                    AddRing(path, projector.ProjectPoints(hole));
                }
            }

            return path;
        }

        private static void AddRing(SKPath path, IReadOnlyList<SKPoint> points)
        {
            if (points == null || points.Count == 0)
                return;

            path.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
                path.LineTo(points[i]);

            path.Close();
        }

        private static void FillSolid(SKCanvas canvas, SKPath path, int argb)
        {
            using (var paint = new SKPaint
            {
                IsStroke = false,
                IsAntialias = true,
                Color = ToColor(argb)
            })
            {
                canvas.DrawPath(path, paint);
            }
        }

        private static void DrawHatch(SKCanvas canvas, SKPath path, SKRect bounds, FillStyleDefinition fill)
        {
            using (var paint = new SKPaint
            {
                IsStroke = true,
                IsAntialias = true,
                Color = ToColor(fill.ForeColorArgb),
                StrokeWidth = (float)Math.Max(fill.HatchThickness, 1d)
            })
            {
                canvas.Save();
                canvas.ClipPath(path, antialias: true);

                var spacing = (float)Math.Max(fill.HatchSpacing, 4d);
                if (fill.FillDirection == FillDirection.Horizontal)
                {
                    for (var y = bounds.Top; y <= bounds.Bottom; y += spacing)
                        canvas.DrawLine(bounds.Left, y, bounds.Right, y, paint);
                }
                else if (fill.FillDirection == FillDirection.Vertical)
                {
                    for (var x = bounds.Left; x <= bounds.Right; x += spacing)
                        canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, paint);
                }
                else if (fill.FillDirection == FillDirection.Diagonal135)
                {
                    for (var x = bounds.Left - bounds.Height; x <= bounds.Right; x += spacing)
                        canvas.DrawLine(x, bounds.Bottom, x + bounds.Height, bounds.Top, paint);
                }
                else
                {
                    for (var x = bounds.Left; x <= bounds.Right + bounds.Height; x += spacing)
                        canvas.DrawLine(x, bounds.Top, x - bounds.Height, bounds.Bottom, paint);
                }

                canvas.Restore();
            }
        }

        private static void DrawPatternTile(SKCanvas canvas, SKPath path, SKRect bounds, FillStyleDefinition fill, int backgroundColorArgb)
        {
            using (var tileBitmap = CreatePatternTileBitmap(fill, backgroundColorArgb))
            using (var paint = new SKPaint
            {
                IsStroke = false,
                IsAntialias = false,
                Color = ToColor(fill.ForeColorArgb),
                FilterQuality = SKFilterQuality.None
            })
            {
                canvas.Save();
                canvas.ClipPath(path, antialias: true);
                DrawBitmapTile(canvas, bounds, tileBitmap, paint);
                canvas.Restore();
            }
        }

        private static void DrawBitmapTile(SKCanvas canvas, SKRect bounds, SKBitmap tileBitmap, SKPaint paint)
        {
            var tileWidth = Math.Max(tileBitmap.Width, 1);
            var tileHeight = Math.Max(tileBitmap.Height, 1);

            for (var y = bounds.Top; y < bounds.Bottom; y += tileHeight)
            {
                for (var x = bounds.Left; x < bounds.Right; x += tileWidth)
                    canvas.DrawBitmap(tileBitmap, x, y, paint);
            }
        }

        private static SKBitmap CreatePatternTileBitmap(FillStyleDefinition fill, int backgroundColorArgb)
        {
            if (fill.TileBytes == null || fill.TileBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Pattern tile '{fill.Id}' must provide non-empty tile bytes.");
            }

            try
            {
                var decoded = SKBitmap.Decode(fill.TileBytes);
                if (decoded != null)
                    return decoded;
            }
            catch (ArgumentNullException)
            {
            }

            var width = Math.Max(fill.TileWidth, 1);
            var height = Math.Max(fill.TileHeight, 1);

            if (fill.TileBytes.Length == width * height)
                return CreateAlphaMaskTile(fill, width, height, backgroundColorArgb);

            if (fill.TileBytes.Length == width * height * 4)
                return CreateArgb32Tile(fill, width, height);

            throw new InvalidOperationException(
                $"Pattern tile '{fill.Id}' must use an encoded image, an alpha mask buffer, or an ARGB32 buffer.");
        }

        private static SKBitmap CreateAlphaMaskTile(FillStyleDefinition fill, int width, int height, int backgroundColorArgb)
        {
            var bitmap = new SKBitmap(width, height, true);
            var backColor = ToColor(fill.BackColorArgb != 0 ? fill.BackColorArgb : backgroundColorArgb);
            var foreColor = ToColor(fill.ForeColorArgb);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var alpha = fill.TileBytes[(y * width) + x];
                    bitmap.SetPixel(x, y, LerpColor(backColor, foreColor, alpha / 255f));
                }
            }

            return bitmap;
        }

        private static SKBitmap CreateArgb32Tile(FillStyleDefinition fill, int width, int height)
        {
            var bitmap = new SKBitmap(width, height, true);
            var bytes = fill.TileBytes;
            var index = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var a = bytes[index++];
                    var r = bytes[index++];
                    var g = bytes[index++];
                    var b = bytes[index++];
                    bitmap.SetPixel(x, y, new SKColor(r, g, b, a));
                }
            }

            return bitmap;
        }

        private static SKShader CreateGradientShader(SKRect rect, FillStyleDefinition fill, int backgroundColorArgb)
        {
            SKPoint start;
            SKPoint end;

            switch (fill.GradientDirection)
            {
                case GradientDirection.TopToBottom:
                    start = new SKPoint(rect.MidX, rect.Top);
                    end = new SKPoint(rect.MidX, rect.Bottom);
                    break;

                case GradientDirection.DiagonalDown:
                    start = new SKPoint(rect.Left, rect.Top);
                    end = new SKPoint(rect.Right, rect.Bottom);
                    break;

                case GradientDirection.DiagonalUp:
                    start = new SKPoint(rect.Left, rect.Bottom);
                    end = new SKPoint(rect.Right, rect.Top);
                    break;

                default:
                    start = new SKPoint(rect.Left, rect.MidY);
                    end = new SKPoint(rect.Right, rect.MidY);
                    break;
            }

            return SKShader.CreateLinearGradient(
                start,
                end,
                new[]
                {
                    ToColor(fill.ForeColorArgb),
                    ToColor(fill.BackColorArgb != 0 ? fill.BackColorArgb : backgroundColorArgb)
                },
                null,
                SKShaderTileMode.Clamp);
        }

        private static SKColor ToColor(int argb)
        {
            var value = unchecked((uint)argb);
            return new SKColor(
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF),
                (byte)((value >> 24) & 0xFF));
        }

        private static SKColor LerpColor(SKColor start, SKColor end, float t)
        {
            var clamped = Math.Max(0f, Math.Min(1f, t));
            return new SKColor(
                (byte)(start.Red + ((end.Red - start.Red) * clamped)),
                (byte)(start.Green + ((end.Green - start.Green) * clamped)),
                (byte)(start.Blue + ((end.Blue - start.Blue) * clamped)),
                (byte)(start.Alpha + ((end.Alpha - start.Alpha) * clamped)));
        }
    }
}
