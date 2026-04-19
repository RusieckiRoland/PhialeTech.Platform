using System;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class SkiaSymbolRenderer
    {
        private readonly SymbolCache _cache;

        public SkiaSymbolRenderer(SymbolCache cache = null)
        {
            _cache = cache ?? new SymbolCache();
        }

        public void DrawSymbol(SKCanvas canvas, IViewport viewport, SymbolRenderRequest request)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Validate();

            var compiled = _cache.GetOrAdd(request.Symbol);
            viewport.ModelToScreen(request.ModelX, request.ModelY, out var screenX, out var screenY);

            var matrix = canvas.TotalMatrix;
            var devicePoint = matrix.MapPoint(screenX, screenY);
            DrawCompiledSymbol(canvas, compiled, devicePoint.X, devicePoint.Y, request.Size, request.RotationDegrees);
        }

        internal CompiledSymbol GetCompiledSymbol(SymbolDefinition symbol)
        {
            return _cache.GetOrAdd(symbol);
        }

        internal static void DrawCompiledSymbol(
            SKCanvas canvas,
            CompiledSymbol compiled,
            float deviceX,
            float deviceY,
            double requestedSize,
            double rotationDegrees)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            if (compiled == null)
                throw new ArgumentNullException(nameof(compiled));

            var scale = ComputeScale(compiled.DefaultSize, requestedSize);

            canvas.Save();
            canvas.ResetMatrix();
            canvas.Translate(deviceX, deviceY);

            if (Math.Abs(rotationDegrees) > double.Epsilon)
                canvas.RotateDegrees((float)rotationDegrees);

            canvas.Scale(scale, scale);
            canvas.Translate((float)-compiled.AnchorX, (float)-compiled.AnchorY);
            canvas.DrawPicture(compiled.Picture);
            canvas.Restore();
        }

        internal static float ComputeScale(double defaultSize, double requestedSize)
        {
            var targetSize = requestedSize > 0d ? requestedSize : defaultSize;
            var sourceSize = defaultSize > 0d ? defaultSize : 1d;
            return (float)(targetSize / sourceSize);
        }
    }
}
