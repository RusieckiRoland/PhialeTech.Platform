using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class SkiaLineStyleRenderer
    {
        private readonly SymbolCache _symbolCache;

        public SkiaLineStyleRenderer(SymbolCache symbolCache = null)
        {
            _symbolCache = symbolCache ?? new SymbolCache();
        }

        public void DrawPolyline(
            SKCanvas canvas,
            SkiaViewportProjector projector,
            IList<PhPoint> points,
            LineTypeDefinition lineType,
            SymbolDefinition stampSymbol = null)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (projector == null)
                throw new ArgumentNullException(nameof(projector));

            if (lineType == null)
                throw new ArgumentNullException(nameof(lineType));

            if (points == null || points.Count < 2)
                return;

            var projectedPoints = projector.ProjectPoints(points);

            if (lineType.Kind == LineTypeKind.VectorStamp)
            {
                if (stampSymbol == null)
                {
                    throw new InvalidOperationException(
                        "Vector stamp line rendering requires the resolved stamp symbol definition.");
                }

                DrawVectorStamp(canvas, projectedPoints, lineType, stampSymbol);
                return;
            }

            if (lineType.Kind == LineTypeKind.RasterPattern)
            {
                DrawRasterPattern(canvas, projectedPoints, lineType);
                return;
            }

            using (var paint = CreatePaint(lineType))
            {
                if (!lineType.Flow && HasDashPattern(lineType))
                {
                    DrawSegmentBySegment(canvas, projectedPoints, paint);
                    return;
                }

                using (var path = BuildPath(projectedPoints))
                {
                    canvas.DrawPath(path, paint);
                }
            }
        }

        private static void DrawRasterPattern(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            LineTypeDefinition lineType)
        {
            var pattern = lineType.RasterPattern;
            if (pattern == null || pattern.Lanes == null || pattern.Lanes.Count == 0)
                return;

            using (var paint = CreateBasePaint(lineType))
            {
                paint.PathEffect = null;
                paint.StrokeCap = SKStrokeCap.Butt;

                for (int i = 0; i < pattern.Lanes.Count; i++)
                {
                    var lane = pattern.Lanes[i];
                    if (lane == null)
                        continue;

                    if (lineType.Flow)
                    {
                        DrawFlowingRasterLane(canvas, points, lane, paint);
                        continue;
                    }

                    DrawSegmentResetRasterLane(canvas, points, lane, paint);
                }
            }
        }

        private void DrawVectorStamp(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            LineTypeDefinition lineType,
            SymbolDefinition stampSymbol)
        {
            if (stampSymbol == null)
                throw new ArgumentNullException(nameof(stampSymbol));

            if (points == null || points.Count < 2)
                return;

            var compiledSymbol = _symbolCache.GetOrAdd(stampSymbol);
            var size = lineType.StampSize > 0d ? lineType.StampSize : stampSymbol.DefaultSize;

            if (lineType.Flow)
            {
                DrawFlowingStamps(canvas, points, lineType, compiledSymbol, size);
                return;
            }

            DrawSegmentResetStamps(canvas, points, lineType, compiledSymbol, size);
        }

        private static void DrawFlowingStamps(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            LineTypeDefinition lineType,
            CompiledSymbol compiledSymbol,
            double size)
        {
            var gap = ResolveStampGap(lineType, size);
            var nextDistance = Math.Max(lineType.InitialGap, 0d);
            var accumulated = 0d;

            for (int i = 1; i < points.Count; i++)
            {
                var start = points[i - 1];
                var end = points[i];
                var segmentLength = Distance(start, end);
                if (segmentLength <= 0.0001d)
                    continue;

                while (nextDistance <= accumulated + segmentLength + 0.0001d)
                {
                    var localDistance = nextDistance - accumulated;
                    var t = Clamp01(localDistance / segmentLength);
                    DrawStamp(canvas, start, end, t, lineType, compiledSymbol, size);
                    nextDistance += gap;
                }

                accumulated += segmentLength;
            }
        }

        private static void DrawSegmentResetStamps(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            LineTypeDefinition lineType,
            CompiledSymbol compiledSymbol,
            double size)
        {
            var gap = ResolveStampGap(lineType, size);
            var initialGap = Math.Max(lineType.InitialGap, 0d);

            for (int i = 1; i < points.Count; i++)
            {
                var start = points[i - 1];
                var end = points[i];
                var segmentLength = Distance(start, end);
                if (segmentLength <= 0.0001d)
                    continue;

                for (var distance = initialGap; distance <= segmentLength + 0.0001d; distance += gap)
                {
                    var t = Clamp01(distance / segmentLength);
                    DrawStamp(canvas, start, end, t, lineType, compiledSymbol, size);
                }
            }
        }

        private static void DrawStamp(
            SKCanvas canvas,
            SKPoint start,
            SKPoint end,
            double t,
            LineTypeDefinition lineType,
            CompiledSymbol compiledSymbol,
            double size)
        {
            var x = Lerp(start.X, end.X, t);
            var y = Lerp(start.Y, end.Y, t);
            var angle = ResolveStampRotation(start, end, lineType);
            SkiaSymbolRenderer.DrawCompiledSymbol(canvas, compiledSymbol, x, y, size, angle);
        }

        private static double ResolveStampGap(LineTypeDefinition lineType, double size)
        {
            if (lineType.Gap > 0d)
                return lineType.Gap;

            if (lineType.Repeat > 0d)
                return lineType.Repeat;

            return Math.Max(size * 2d, 1d);
        }

        private static double ResolveStampRotation(SKPoint start, SKPoint end, LineTypeDefinition lineType)
        {
            var angle = 0d;
            if (lineType.OrientToTangent)
                angle = Math.Atan2(end.Y - start.Y, end.X - start.X) * (180d / Math.PI);

            if (lineType.Perpendicular)
                angle += 90d;

            return angle;
        }

        private static double Distance(SKPoint start, SKPoint end)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static float Lerp(float start, float end, double t)
        {
            return (float)(start + ((end - start) * Clamp01(t)));
        }

        private static SKPoint Lerp(SKPoint start, SKPoint end, double t)
        {
            return new SKPoint(Lerp(start.X, end.X, t), Lerp(start.Y, end.Y, t));
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
                return 0d;

            if (value > 1d)
                return 1d;

            return value;
        }

        private static void DrawSegmentBySegment(SKCanvas canvas, IReadOnlyList<SKPoint> points, SKPaint paint)
        {
            for (int i = 1; i < points.Count; i++)
            {
                using (var path = new SKPath())
                {
                    path.MoveTo(points[i - 1]);
                    path.LineTo(points[i]);
                    canvas.DrawPath(path, paint);
                }
            }
        }

        private static SKPath BuildPath(IReadOnlyList<SKPoint> points)
        {
            var path = new SKPath();
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
                path.LineTo(points[i]);

            return path;
        }

        private static SKPaint CreatePaint(LineTypeDefinition lineType)
        {
            var paint = CreateBasePaint(lineType);

            var dashPattern = ResolveDashPattern(lineType);
            if (dashPattern != null)
                paint.PathEffect = SKPathEffect.CreateDash(dashPattern, (float)lineType.DashOffset);

            return paint;
        }

        private static SKPaint CreateBasePaint(LineTypeDefinition lineType)
        {
            return new SKPaint
            {
                IsStroke = true,
                IsAntialias = true,
                Color = FromArgb(lineType.ColorArgb),
                StrokeWidth = (float)Math.Max(lineType.Width, 0d),
                StrokeCap = MapLineCap(lineType.Linecap),
                StrokeJoin = MapLineJoin(lineType.Linejoin),
                StrokeMiter = (float)Math.Max(lineType.MiterLimit, 1d)
            };
        }

        private static bool HasDashPattern(LineTypeDefinition lineType)
        {
            return ResolveDashPattern(lineType) != null;
        }

        private static float[] ResolveDashPattern(LineTypeDefinition lineType)
        {
            if (lineType.Kind != LineTypeKind.SimpleDash)
                return null;

            if (lineType.DashPattern != null && lineType.DashPattern.Length >= 2)
            {
                var dashPattern = new float[lineType.DashPattern.Length];
                for (int i = 0; i < lineType.DashPattern.Length; i++)
                    dashPattern[i] = (float)Math.Max(lineType.DashPattern[i], 0.1d);

                return dashPattern;
            }

            if (lineType.Repeat > 0d)
            {
                var unit = (float)Math.Max(lineType.Repeat / 2d, 0.1d);
                return new[] { unit, unit };
            }

            return null;
        }

        private static void DrawFlowingRasterLane(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            RasterLinePatternLane lane,
            SKPaint paint)
        {
            var state = new RasterLaneState(lane);
            for (int i = 1; i < points.Count; i++)
                DrawRasterLaneSegment(canvas, points[i - 1], points[i], lane.OffsetY, paint, state);
        }

        private static void DrawSegmentResetRasterLane(
            SKCanvas canvas,
            IReadOnlyList<SKPoint> points,
            RasterLinePatternLane lane,
            SKPaint paint)
        {
            for (int i = 1; i < points.Count; i++)
            {
                var state = new RasterLaneState(lane);
                DrawRasterLaneSegment(canvas, points[i - 1], points[i], lane.OffsetY, paint, state);
            }
        }

        private static void DrawRasterLaneSegment(
            SKCanvas canvas,
            SKPoint start,
            SKPoint end,
            double offsetY,
            SKPaint paint,
            RasterLaneState state)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var segmentLength = Math.Sqrt((dx * dx) + (dy * dy));
            if (segmentLength <= 0.0001d)
                return;

            var normalX = (float)(-dy / segmentLength);
            var normalY = (float)(dx / segmentLength);

            var offsetStart = new SKPoint(start.X + (normalX * (float)offsetY), start.Y + (normalY * (float)offsetY));
            var offsetEnd = new SKPoint(end.X + (normalX * (float)offsetY), end.Y + (normalY * (float)offsetY));

            if (!state.HasPattern)
            {
                canvas.DrawLine(offsetStart, offsetEnd, paint);
                return;
            }

            var consumed = 0d;
            while (consumed < segmentLength - 0.0001d)
            {
                var chunk = Math.Min(state.RemainingLength, segmentLength - consumed);
                if (state.IsDash)
                {
                    var from = consumed / segmentLength;
                    var to = (consumed + chunk) / segmentLength;
                    var fromPoint = Lerp(offsetStart, offsetEnd, from);
                    var toPoint = Lerp(offsetStart, offsetEnd, to);
                    canvas.DrawLine(
                        fromPoint.X,
                        fromPoint.Y,
                        toPoint.X,
                        toPoint.Y,
                        paint);
                }

                consumed += chunk;
                state.Advance(chunk);
            }
        }

        private sealed class RasterLaneState
        {
            private readonly int[] _runLengths;
            private int _index;

            public RasterLaneState(RasterLinePatternLane lane)
            {
                _runLengths = lane != null && lane.RunLengths != null ? lane.RunLengths : Array.Empty<int>();
                HasPattern = _runLengths.Length > 0;
                IsDash = lane == null || lane.StartsWithDash;
                RemainingLength = HasPattern ? Math.Max(_runLengths[0], 1) : 0d;
            }

            public bool HasPattern { get; }

            public bool IsDash { get; private set; }

            public double RemainingLength { get; private set; }

            public void Advance(double length)
            {
                if (!HasPattern)
                    return;

                RemainingLength -= length;
                while (RemainingLength <= 0d)
                {
                    var overshoot = -RemainingLength;
                    _index = (_index + 1) % _runLengths.Length;
                    RemainingLength = Math.Max(_runLengths[_index], 1) - overshoot;
                    IsDash = !IsDash;
                }
            }
        }

        private static SKColor FromArgb(int argb)
        {
            var value = unchecked((uint)argb);
            var a = (byte)((value >> 24) & 0xFF);
            var r = (byte)((value >> 16) & 0xFF);
            var g = (byte)((value >> 8) & 0xFF);
            var b = (byte)(value & 0xFF);
            return new SKColor(r, g, b, a);
        }

        private static SKStrokeCap MapLineCap(StrokeLinecap linecap)
        {
            switch (linecap)
            {
                case StrokeLinecap.Round:
                    return SKStrokeCap.Round;
                case StrokeLinecap.Square:
                    return SKStrokeCap.Square;
                default:
                    return SKStrokeCap.Butt;
            }
        }

        private static SKStrokeJoin MapLineJoin(StrokeLinejoin linejoin)
        {
            switch (linejoin)
            {
                case StrokeLinejoin.Round:
                    return SKStrokeJoin.Round;
                case StrokeLinejoin.Bevel:
                    return SKStrokeJoin.Bevel;
                default:
                    return SKStrokeJoin.Miter;
            }
        }
    }
}
