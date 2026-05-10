using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.Styling;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class SkiaFillStyleRendererTests
    {
        [Test]
        public void FillPolygon_WithGradient_ProducesDifferentColorsAcrossShape()
        {
            using (var bitmap = new SKBitmap(64, 64, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var projector = new SkiaViewportProjector(new IdentityViewport());
                projector.PrepareMatrix();

                var renderer = new SkiaFillStyleRenderer();
                renderer.FillPolygon(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, 8d),
                        new PhPoint(56d, 8d),
                        new PhPoint(56d, 56d),
                        new PhPoint(8d, 8d)
                    },
                    null,
                    new FillStyleDefinition
                    {
                        Kind = FillStyleKind.Gradient,
                        ForeColorArgb = unchecked((int)0xFF163046u),
                        BackColorArgb = unchecked((int)0xFFFFFFFFu),
                        GradientDirection = GradientDirection.LeftToRight
                    },
                    unchecked((int)0xFFFFFFFFu));

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(16, 32), Is.Not.EqualTo(bitmap.GetPixel(48, 32)));
            }
        }

        [Test]
        public void FillPolygon_WithHatch_ProducesVisibleForegroundMarks()
        {
            using (var bitmap = new SKBitmap(64, 64, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var projector = new SkiaViewportProjector(new IdentityViewport());
                projector.PrepareMatrix();

                var renderer = new SkiaFillStyleRenderer();
                renderer.FillPolygon(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, 8d),
                        new PhPoint(56d, 8d),
                        new PhPoint(56d, 56d),
                        new PhPoint(8d, 8d)
                    },
                    null,
                    new FillStyleDefinition
                    {
                        Kind = FillStyleKind.Hatch,
                        ForeColorArgb = unchecked((int)0xFF163046u),
                        BackColorArgb = unchecked((int)0xFFFFFFFFu),
                        FillDirection = FillDirection.Diagonal45,
                        HatchSpacing = 6d,
                        HatchThickness = 1d
                    },
                    unchecked((int)0xFFFFFFFFu));

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(20, 20), Is.Not.EqualTo(SKColors.Transparent));
            }
        }

        [Test]
        public void FillPolygon_WithPatternTileBytes_RendersTileForegroundAndBackground()
        {
            using (var bitmap = new SKBitmap(32, 32, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var projector = new SkiaViewportProjector(new IdentityViewport());
                projector.PrepareMatrix();

                var renderer = new SkiaFillStyleRenderer();
                renderer.FillPolygon(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, 8d),
                        new PhPoint(24d, 8d),
                        new PhPoint(24d, 24d),
                        new PhPoint(8d, 24d),
                        new PhPoint(8d, 8d)
                    },
                    null,
                    new FillStyleDefinition
                    {
                        Id = "tile",
                        Kind = FillStyleKind.PatternTile,
                        ForeColorArgb = unchecked((int)0xFF204060u),
                        BackColorArgb = unchecked((int)0xFFFFFFFFu),
                        TileWidth = 2,
                        TileHeight = 2,
                        TileBytes = new byte[] { 255, 0, 0, 255 }
                    },
                    unchecked((int)0xFFFFFFFFu));

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(10, 10), Is.EqualTo(new SKColor(32, 64, 96, 255)));
                Assert.That(bitmap.GetPixel(11, 10), Is.EqualTo(SKColors.White));
            }
        }

        [Test]
        public void FillPolygon_WithUnsupportedPatternTileBytes_Throws()
        {
            using (var bitmap = new SKBitmap(32, 32, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                var projector = new SkiaViewportProjector(new IdentityViewport());
                projector.PrepareMatrix();

                var renderer = new SkiaFillStyleRenderer();

                Assert.That(
                    () => renderer.FillPolygon(
                        canvas,
                        projector,
                        new[]
                        {
                            new PhPoint(8d, 8d),
                            new PhPoint(24d, 8d),
                            new PhPoint(24d, 24d),
                            new PhPoint(8d, 24d),
                            new PhPoint(8d, 8d)
                        },
                        null,
                        new FillStyleDefinition
                        {
                            Id = "broken-tile",
                            Kind = FillStyleKind.PatternTile,
                            ForeColorArgb = unchecked((int)0xFF204060u),
                            BackColorArgb = unchecked((int)0xFFFFFFFFu),
                            TileWidth = 3,
                            TileHeight = 3,
                            TileBytes = new byte[] { 1, 2, 3, 4, 5 }
                        },
                        unchecked((int)0xFFFFFFFFu)),
                    Throws.TypeOf<System.InvalidOperationException>());

                projector.ReleaseMatrix();
            }
        }

        private sealed class IdentityViewport : Abstractions.Ui.Rendering.IViewport
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

