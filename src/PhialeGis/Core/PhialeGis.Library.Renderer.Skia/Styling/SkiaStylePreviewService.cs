using System;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Renderer.Skia.Styling
{
    public sealed class SkiaStylePreviewService : IStylePreviewService
    {
        private readonly StylePreviewCache _cache;
        private readonly SkiaLineStyleRenderer _lineRenderer;
        private readonly SkiaSymbolRenderer _symbolRenderer;
        private readonly SkiaFillStyleRenderer _fillRenderer;
        private readonly IStyleContrastPolicy _contrastPolicy;

        public SkiaStylePreviewService(
            StylePreviewCache cache = null,
            SkiaLineStyleRenderer lineRenderer = null,
            SkiaSymbolRenderer symbolRenderer = null,
            SkiaFillStyleRenderer fillRenderer = null,
            IStyleContrastPolicy contrastPolicy = null)
        {
            _cache = cache ?? new StylePreviewCache();
            _lineRenderer = lineRenderer ?? new SkiaLineStyleRenderer();
            _symbolRenderer = symbolRenderer ?? new SkiaSymbolRenderer();
            _fillRenderer = fillRenderer ?? new SkiaFillStyleRenderer();
            _contrastPolicy = contrastPolicy ?? new DefaultStyleContrastPolicy();
        }

        public StylePreviewImage Render(StylePreviewRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.Validate();
            return _cache.GetOrAdd(request, () => RenderCore(request));
        }

        private StylePreviewImage RenderCore(StylePreviewRequest request)
        {
            var width = Math.Max(request.WidthPx, 8);
            var height = Math.Max(request.HeightPx, 8);

            using (var bitmap = new SKBitmap(width, height, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(ToColor(request.BackgroundColorArgb));

                switch (request.Kind)
                {
                    case StylePreviewKind.Line:
                        RenderLinePreview(canvas, request, width, height);
                        break;

                    case StylePreviewKind.Symbol:
                        RenderSymbolPreview(canvas, request, width, height);
                        break;

                    case StylePreviewKind.Fill:
                        RenderFillPreview(canvas, request, width, height);
                        break;
                }

                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return new StylePreviewImage
                    {
                        WidthPx = width,
                        HeightPx = height,
                        PngBytes = data.ToArray()
                    };
                }
            }
        }

        private void RenderLinePreview(SKCanvas canvas, StylePreviewRequest request, int width, int height)
        {
            var lineType = request.LineType;
            var viewport = new PreviewViewport();
            var projector = new SkiaViewportProjector(viewport);
            projector.PrepareMatrix();
            var backgroundArgb = request.BackgroundColorArgb;

            if (lineType.Kind == LineTypeKind.VectorStamp && request.LineStampSymbol != null && ShouldUseSymbolHalo(request.LineStampSymbol, backgroundArgb))
            {
                _lineRenderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, height / 2d),
                        new PhPoint(width - 8d, height / 2d)
                    },
                    CreateHaloLineType(lineType, backgroundArgb),
                    CreateHaloSymbol(request.LineStampSymbol, backgroundArgb));
            }
            else if (lineType.Kind != LineTypeKind.VectorStamp && _contrastPolicy.ShouldApplyHalo(lineType.ColorArgb, backgroundArgb))
            {
                _lineRenderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, height / 2d),
                        new PhPoint(width - 8d, height / 2d)
                    },
                    CreateHaloLineType(lineType, backgroundArgb));
            }

            _lineRenderer.DrawPolyline(
                canvas,
                projector,
                new[]
                {
                    new PhPoint(8d, height / 2d),
                    new PhPoint(width - 8d, height / 2d)
                },
                lineType,
                request.LineStampSymbol);

            projector.ReleaseMatrix();
        }

        private void RenderSymbolPreview(SKCanvas canvas, StylePreviewRequest request, int width, int height)
        {
            var symbol = request.Symbol;
            if (symbol == null)
                return;

            if (ShouldUseSymbolHalo(symbol, request.BackgroundColorArgb))
            {
                _symbolRenderer.DrawSymbol(
                    canvas,
                    new PreviewViewport(),
                    new SymbolRenderRequest
                    {
                        ModelX = width / 2d,
                        ModelY = height / 2d,
                        Size = (Math.Min(width, height) * 0.6d) + 2d,
                        Symbol = CreateHaloSymbol(symbol, request.BackgroundColorArgb)
                    });
            }

            _symbolRenderer.DrawSymbol(
                canvas,
                new PreviewViewport(),
                new SymbolRenderRequest
                {
                    ModelX = width / 2d,
                    ModelY = height / 2d,
                    Size = Math.Min(width, height) * 0.6d,
                    Symbol = symbol
                });
        }

        private void RenderFillPreview(SKCanvas canvas, StylePreviewRequest request, int width, int height)
        {
            var fill = request.FillStyle;
            var rect = new SKRect(6f, 6f, width - 6f, height - 6f);
            var borderColor = _contrastPolicy.GetBorderColorArgb(request.BackgroundColorArgb);

            using (var path = new SKPath())
            {
                path.AddRect(rect);
                _fillRenderer.FillPath(canvas, path, fill, request.BackgroundColorArgb);

                using (var border = new SKPaint
                {
                    IsStroke = true,
                    IsAntialias = true,
                    Color = ToColor(borderColor),
                    StrokeWidth = 1f
                })
                {
                    canvas.DrawPath(path, border);
                }
            }
        }

        private LineTypeDefinition CreateHaloLineType(LineTypeDefinition lineType, int backgroundArgb)
        {
            return new LineTypeDefinition
            {
                Id = (lineType.Id ?? string.Empty) + "-preview-halo",
                Name = lineType.Name,
                Kind = lineType.Kind,
                ColorArgb = _contrastPolicy.GetHaloColorArgb(backgroundArgb),
                Width = Math.Max(lineType.Width + 2d, 3d),
                Flow = lineType.Flow,
                Repeat = lineType.Repeat,
                Linecap = lineType.Linecap,
                Linejoin = lineType.Linejoin,
                MiterLimit = lineType.MiterLimit,
                DashPattern = lineType.DashPattern,
                DashOffset = lineType.DashOffset,
                RasterPattern = lineType.RasterPattern,
                SymbolId = lineType.SymbolId,
                StampSize = Math.Max(lineType.StampSize + 2d, lineType.StampSize),
                Gap = lineType.Gap,
                InitialGap = lineType.InitialGap,
                OrientToTangent = lineType.OrientToTangent,
                Perpendicular = lineType.Perpendicular
            };
        }

        private SymbolDefinition CreateHaloSymbol(SymbolDefinition symbol, int backgroundArgb)
        {
            var haloColor = _contrastPolicy.GetHaloColorArgb(backgroundArgb);
            var primitives = new StylePrimitive[symbol.Primitives.Count];

            for (int i = 0; i < symbol.Primitives.Count; i++)
            {
                var primitive = symbol.Primitives[i];
                primitives[i] = new StylePrimitive
                {
                    Kind = primitive.Kind,
                    Coordinates = primitive.Coordinates,
                    StrokeColorArgb = haloColor,
                    FillColorArgb = ((uint)primitive.FillColorArgb >> 24) != 0 ? haloColor : 0,
                    StrokeWidth = Math.Max(primitive.StrokeWidth + 1.5d, 2d)
                };
            }

            return new SymbolDefinition
            {
                Id = (symbol.Id ?? string.Empty) + "-preview-halo",
                Name = symbol.Name,
                AnchorX = symbol.AnchorX,
                AnchorY = symbol.AnchorY,
                DefaultSize = symbol.DefaultSize,
                Primitives = primitives
            };
        }

        private bool ShouldUseSymbolHalo(SymbolDefinition symbol, int backgroundArgb)
        {
            for (int i = 0; i < symbol.Primitives.Count; i++)
            {
                var primitive = symbol.Primitives[i];
                if (((uint)primitive.StrokeColorArgb >> 24) != 0 && _contrastPolicy.ShouldApplyHalo(primitive.StrokeColorArgb, backgroundArgb))
                    return true;

                if (((uint)primitive.FillColorArgb >> 24) != 0 && _contrastPolicy.ShouldApplyHalo(primitive.FillColorArgb, backgroundArgb))
                    return true;
            }

            return false;
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

        private sealed class PreviewViewport : IViewport
        {
            public double Scale => 1d;

            public double GetDpiX() => 96d;

            public double GetDpiY() => 96d;

            public void ModelToScreen(double modelX, double modelY, out float screenX, out float screenY)
            {
                screenX = (float)modelX;
                screenY = (float)modelY;
            }

            public bool Zoom(double factor) => true;

            public bool PanByScreenOffset(double dx, double dy) => true;

            public void GetModelToScreenAffine(
                out double m11,
                out double m12,
                out double m21,
                out double m22,
                out double tx,
                out double ty)
            {
                m11 = 1d;
                m12 = 0d;
                m21 = 0d;
                m22 = 1d;
                tx = 0d;
                ty = 0d;
            }

            public void GetScreenToModelAffine(
                out double m11,
                out double m12,
                out double m21,
                out double m22,
                out double tx,
                out double ty)
            {
                m11 = 1d;
                m12 = 0d;
                m21 = 0d;
                m22 = 1d;
                tx = 0d;
                ty = 0d;
            }
        }
    }
}
