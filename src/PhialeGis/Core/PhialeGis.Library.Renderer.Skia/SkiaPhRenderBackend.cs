// PhialeGis.Library.Renderer.Skia/SkiaPhRenderBackend.cs
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Render;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Systems;
using PhialeGis.Library.Renderer.Skia.Styling;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PhialeGis.Library.Renderer.Skia
{
    public sealed class SkiaPhRenderBackend : IPhRenderBackend, IPhRenderDriver, IPhScreenSpaceBackend, ISymbolRenderDriver, ILineStyleRenderDriver, IFillStyleRenderDriver, IStyledOverlayRenderBackend
    {
        private const int CanvasBackgroundColorArgb = unchecked((int)0xFFFFFFFF);
        private readonly SKCanvas _canvas;

        private readonly IViewport _viewport;                 // single-point fast projection
        private readonly SkiaViewportProjector _projector;    // bulk projection
        private readonly SkiaSymbolRenderer _symbolRenderer;
        private readonly SkiaLineStyleRenderer _lineStyleRenderer;
        private readonly SkiaFillStyleRenderer _fillStyleRenderer;
        private readonly SymbolCache _symbolCache;

        // --- DIAG ---
        private bool _loggedMatrix;

        [Conditional("DEBUG")]
        private void LogCanvasMatrix(string where)
        {
            if (_loggedMatrix) return;
            _loggedMatrix = true;
            var m = _canvas.TotalMatrix;
            Debug.WriteLine(
                "[SKIA:" + where + $"] M=[{m.ScaleX}, {m.SkewX}, {m.TransX}; {m.SkewY}, {m.ScaleY}, {m.TransY}; {m.Persp0}, {m.Persp1}, {m.Persp2}] " +
                $"scale=({m.ScaleX},{m.ScaleY}) trans=({m.TransX},{m.TransY})");
        }
        // -----------

        public SkiaPhRenderBackend(SKCanvas canvas, IViewport viewport)
        {
            if (canvas == null) throw new ArgumentNullException("canvas");
            if (viewport == null) throw new ArgumentNullException("viewport");

            _canvas = canvas;
            _viewport = viewport;
            _projector = new SkiaViewportProjector(viewport);
            _symbolCache = new SymbolCache();
            _symbolRenderer = new SkiaSymbolRenderer(_symbolCache);
            _lineStyleRenderer = new SkiaLineStyleRenderer(_symbolCache);
            _fillStyleRenderer = new SkiaFillStyleRenderer();

            LogCanvasMatrix("ctor");
        }

        public void BeginUpdate()
        {
            _canvas.Clear(SKColors.White);
            _projector.PrepareMatrix();
        }

        public void EndUpdate()
        {
            _projector.ReleaseMatrix();

            // Draw corner markers in device pixels (debug)
            DrawCornerMarkers(lengthPx: 24, marginPx: 0, colorArgb: 0xFF000000, thicknessPx: 4f);
        }

        public void DrawSymbol(SymbolRenderRequest request)
        {
            LogCanvasMatrix("DrawSymbol");
            _symbolRenderer.DrawSymbol(_canvas, _viewport, request);
        }

        public void DrawStyledLine(LineRenderRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Validate();
            LogCanvasMatrix("DrawStyledLine");
            _lineStyleRenderer.DrawPolyline(_canvas, _projector, request.Points, request.LineType, request.StampSymbol);
        }

        public void DrawOverlayStyledLine(OverlayLineRenderRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Validate();
            LogCanvasMatrix("DrawOverlayStyledLine");
            _lineStyleRenderer.DrawPolyline(_canvas, _projector, request.Points, request.LineType, request.StampSymbol);
        }

        public void FillPolygon(FillRenderRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Validate();
            LogCanvasMatrix("FillPolygonStyled");
            _fillStyleRenderer.FillPolygon(
                _canvas,
                _projector,
                request.Outer,
                request.Holes,
                request.FillStyle,
                CanvasBackgroundColorArgb);
        }

        private static SKColor FromArgb(uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return new SKColor(r, g, b, a);
        }

        public void DrawScreenLine(float x1, float y1, float x2, float y2, uint strokeArgb, float thicknessPx)
        {
            var matrix = _canvas.TotalMatrix;
            var p1 = matrix.MapPoint(x1, y1);
            var p2 = matrix.MapPoint(x2, y2);
            var scaledThickness = ScaleScreenThickness(thicknessPx, matrix);

            using (var p = new SKPaint
            {
                IsStroke = true,
                IsAntialias = false,
                Color = FromArgb(strokeArgb),
                StrokeWidth = scaledThickness,
                StrokeCap = SKStrokeCap.Butt
            })
            {
                _canvas.Save();
                _canvas.ResetMatrix();
                _canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, p);
                _canvas.Restore();
            }
        }

        public void DrawScreenRect(float x, float y, float width, float height, uint strokeArgb, float thicknessPx)
        {
            var matrix = _canvas.TotalMatrix;
            var p1 = matrix.MapPoint(x, y);
            var p2 = matrix.MapPoint(x + width, y + height);
            var left = Math.Min(p1.X, p2.X);
            var top = Math.Min(p1.Y, p2.Y);
            var right = Math.Max(p1.X, p2.X);
            var bottom = Math.Max(p1.Y, p2.Y);
            var scaledThickness = ScaleScreenThickness(thicknessPx, matrix);

            using (var p = new SKPaint
            {
                IsStroke = true,
                IsAntialias = false,
                Color = FromArgb(strokeArgb),
                StrokeWidth = scaledThickness,
                StrokeCap = SKStrokeCap.Butt
            })
            {
                _canvas.Save();
                _canvas.ResetMatrix();
                _canvas.DrawRect(left, top, right - left, bottom - top, p);
                _canvas.Restore();
            }
        }

        private static float ScaleScreenThickness(float thickness, SKMatrix matrix)
        {
            if (thickness <= 0f) return 0f;

            // Effective scale from local screen-space to device pixels.
            var sx = (float)Math.Sqrt(matrix.ScaleX * matrix.ScaleX + matrix.SkewY * matrix.SkewY);
            var sy = (float)Math.Sqrt(matrix.ScaleY * matrix.ScaleY + matrix.SkewX * matrix.SkewX);
            var s = Math.Max(sx, sy);
            if (float.IsNaN(s) || float.IsInfinity(s) || s <= 0f)
                s = 1f;

            return thickness * s;
        }

        /// <summary>
        /// Draws L-shaped corner markers in device pixels, independent of canvas transforms.
        /// </summary>
        private void DrawCornerMarkers(int lengthPx, int marginPx, uint colorArgb, float thicknessPx)
        {
            // Use device pixel space: crisp lines, unaffected by model/view transforms
            using (var p = new SKPaint
            {
                IsStroke = true,
                IsAntialias = false,
                Color = FromArgb(colorArgb),
                StrokeWidth = thicknessPx,
                StrokeCap = SKStrokeCap.Butt
            })
            {
                _canvas.Save();
                _canvas.ResetMatrix();

                // Device-space clip rectangle (pixels)
                var rc = _canvas.DeviceClipBounds;

                int left = rc.Left + marginPx;
                int top = rc.Top + marginPx;
                int right = rc.Right - 1 - marginPx;
                int bottom = rc.Bottom - 1 - marginPx;
                int L = lengthPx > 0 ? lengthPx : 1;

                // bottom-left "_|"
                _canvas.DrawLine((float)left, (float)bottom, (float)(left + L), (float)bottom, p);      // horizontal
                _canvas.DrawLine((float)left, (float)(bottom - L), (float)left, (float)bottom, p);      // vertical

                // top-left "┌"
                _canvas.DrawLine((float)left, (float)top, (float)(left + L), (float)top, p);
                _canvas.DrawLine((float)left, (float)top, (float)left, (float)(top + L), p);

                // top-right "┐"
                _canvas.DrawLine((float)(right - L), (float)top, (float)right, (float)top, p);
                _canvas.DrawLine((float)right, (float)top, (float)right, (float)(top + L), p);

                // bottom-right "┘"
                _canvas.DrawLine((float)(right - L), (float)bottom, (float)right, (float)bottom, p);
                _canvas.DrawLine((float)right, (float)(bottom - L), (float)right, (float)bottom, p);

                _canvas.Restore();
            }
        }
    }
}
