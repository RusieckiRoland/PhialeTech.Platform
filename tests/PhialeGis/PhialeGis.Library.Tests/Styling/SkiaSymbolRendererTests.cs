using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Renderer.Skia.Styling;
using SkiaSharp;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class SkiaSymbolRendererTests
    {
        [Test]
        public void DrawSymbol_RendersVisiblePixelsNearProjectedPoint()
        {
            using (var bitmap = new SKBitmap(64, 64, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var renderer = new SkiaSymbolRenderer();
                renderer.DrawSymbol(
                    canvas,
                    new IdentityViewport(),
                    new SymbolRenderRequest
                    {
                        ModelX = 20d,
                        ModelY = 30d,
                        Size = 12d,
                        Symbol = new SymbolDefinition
                        {
                            Id = "square",
                            AnchorX = 4d,
                            AnchorY = 4d,
                            DefaultSize = 8d,
                            Primitives = new StylePrimitive[]
                            {
                                new StylePrimitive
                                {
                                    Kind = SymbolPrimitiveKind.Polygon,
                                    Coordinates = new[] { 0d, 0d, 8d, 0d, 8d, 8d, 0d, 8d },
                                    StrokeColorArgb = unchecked((int)0xFF163046u),
                                    FillColorArgb = unchecked((int)0xFFFFFFFFu),
                                    StrokeWidth = 1d
                                }
                            }
                        }
                    });

                var centerPixel = bitmap.GetPixel(20, 30);
                Assert.That(centerPixel.Alpha, Is.GreaterThan((byte)0));
            }
        }

        [Test]
        public void DrawSymbol_WithRotation_StillProducesVisiblePixels()
        {
            using (var bitmap = new SKBitmap(64, 64, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var renderer = new SkiaSymbolRenderer();
                renderer.DrawSymbol(
                    canvas,
                    new IdentityViewport(),
                    new SymbolRenderRequest
                    {
                        ModelX = 32d,
                        ModelY = 32d,
                        Size = 16d,
                        RotationDegrees = 45d,
                        Symbol = new SymbolDefinition
                        {
                            Id = "triangle",
                            AnchorX = 4d,
                            AnchorY = 4d,
                            DefaultSize = 8d,
                            Primitives = new StylePrimitive[]
                            {
                                new StylePrimitive
                                {
                                    Kind = SymbolPrimitiveKind.Polygon,
                                    Coordinates = new[] { 4d, 0d, 8d, 8d, 0d, 8d },
                                    StrokeColorArgb = unchecked((int)0xFF204060u),
                                    FillColorArgb = unchecked((int)0xFF90B0D0u),
                                    StrokeWidth = 1d
                                }
                            }
                        }
                    });

                Assert.That(HasVisiblePixels(bitmap), Is.True);
            }
        }

        private static bool HasVisiblePixels(SKBitmap bitmap)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).Alpha > 0)
                        return true;
                }
            }

            return false;
        }

        private sealed class IdentityViewport : IViewport
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
