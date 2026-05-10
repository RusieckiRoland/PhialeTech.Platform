using System.Linq;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.Styling;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class SkiaLineStyleRendererTests
    {
        [Test]
        public void DrawPolyline_WithDashPattern_CreatesVisibleGaps()
        {
            using (var bitmap = new SKBitmap(80, 24, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var viewport = new IdentityViewport();
                var projector = new SkiaViewportProjector(viewport);
                projector.PrepareMatrix();

                var renderer = new SkiaLineStyleRenderer();
                renderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(5d, 12d),
                        new PhPoint(75d, 12d)
                    },
                    new LineTypeDefinition
                    {
                        Id = "dash",
                        Kind = LineTypeKind.SimpleDash,
                        ColorArgb = unchecked((int)0xFF204060u),
                        Width = 2d,
                        DashPattern = new[] { 6d, 4d },
                        Flow = true
                    });

                projector.ReleaseMatrix();

                var alphas = Enumerable.Range(5, 71).Select(x => bitmap.GetPixel(x, 12).Alpha).ToArray();
                Assert.That(alphas.Any(a => a > 0), Is.True);
                Assert.That(alphas.Any(a => a == 0), Is.True);
            }
        }

        [Test]
        public void DrawPolyline_WithNullLineType_Throws()
        {
            using (var bitmap = new SKBitmap(32, 32, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                var renderer = new SkiaLineStyleRenderer();
                var projector = new SkiaViewportProjector(new IdentityViewport());
                projector.PrepareMatrix();

                Assert.That(
                    () => renderer.DrawPolyline(
                        canvas,
                        projector,
                        new[]
                        {
                            new PhPoint(4d, 16d),
                            new PhPoint(28d, 16d)
                        },
                        null),
                    Throws.TypeOf<System.ArgumentNullException>());

                projector.ReleaseMatrix();
            }
        }

        [Test]
        public void DrawPolyline_WithFlowDisabled_DrawsEachSegmentWithResetPattern()
        {
            using (var bitmap = new SKBitmap(80, 80, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var viewport = new IdentityViewport();
                var projector = new SkiaViewportProjector(viewport);
                projector.PrepareMatrix();

                var renderer = new SkiaLineStyleRenderer();
                renderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(10d, 10d),
                        new PhPoint(10d, 40d),
                        new PhPoint(40d, 40d)
                    },
                    new LineTypeDefinition
                    {
                        Id = "dash-reset",
                        Kind = LineTypeKind.SimpleDash,
                        ColorArgb = unchecked((int)0xFF204060u),
                        Width = 2d,
                        DashPattern = new[] { 5d, 5d },
                        Flow = false
                    });

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(10, 12).Alpha, Is.GreaterThan((byte)0));
                Assert.That(bitmap.GetPixel(12, 40).Alpha, Is.GreaterThan((byte)0));
            }
        }

        [Test]
        public void DrawPolyline_WithVectorStamp_RendersVisibleStampPixels()
        {
            using (var bitmap = new SKBitmap(96, 32, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var viewport = new IdentityViewport();
                var projector = new SkiaViewportProjector(viewport);
                projector.PrepareMatrix();

                var renderer = new SkiaLineStyleRenderer();
                renderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, 16d),
                        new PhPoint(88d, 16d)
                    },
                    new LineTypeDefinition
                    {
                        Id = "ticks-perp",
                        Kind = LineTypeKind.VectorStamp,
                        Gap = 12d,
                        InitialGap = 0d,
                        StampSize = 8d,
                        OrientToTangent = true,
                        Perpendicular = true
                    },
                    new SymbolDefinition
                    {
                        Id = "tick",
                        AnchorX = 4d,
                        AnchorY = 4d,
                        DefaultSize = 8d,
                        Primitives = new StylePrimitive[]
                        {
                            new StylePrimitive
                            {
                                Kind = SymbolPrimitiveKind.Polyline,
                                Coordinates = new[] { 0d, 4d, 8d, 4d },
                                StrokeColorArgb = unchecked((int)0xFF204060u),
                                FillColorArgb = 0,
                                StrokeWidth = 1d
                            }
                        }
                    });

                projector.ReleaseMatrix();

                Assert.That(HasVisiblePixels(bitmap), Is.True);
                Assert.That(bitmap.GetPixel(8, 12).Alpha, Is.GreaterThan((byte)0));
            }
        }

        [Test]
        public void DrawPolyline_WithVectorStampAndMissingSymbol_Throws()
        {
            using (var bitmap = new SKBitmap(32, 32, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                var renderer = new SkiaLineStyleRenderer();
                var projector = new SkiaViewportProjector(new IdentityViewport());
                projector.PrepareMatrix();

                Assert.That(
                    () => renderer.DrawPolyline(
                        canvas,
                        projector,
                        new[]
                        {
                            new PhPoint(4d, 16d),
                            new PhPoint(28d, 16d)
                        },
                        new LineTypeDefinition
                        {
                            Id = "ticks-perp",
                            Kind = LineTypeKind.VectorStamp,
                            Gap = 12d,
                            StampSize = 8d
                        }),
                    Throws.TypeOf<System.InvalidOperationException>());

                projector.ReleaseMatrix();
            }
        }

        [Test]
        public void DrawPolyline_WithVectorStampAndFlowDisabled_RestartsPatternPerSegment()
        {
            using (var bitmap = new SKBitmap(80, 80, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var viewport = new IdentityViewport();
                var projector = new SkiaViewportProjector(viewport);
                projector.PrepareMatrix();

                var renderer = new SkiaLineStyleRenderer();
                renderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(10d, 10d),
                        new PhPoint(10d, 40d),
                        new PhPoint(40d, 40d)
                    },
                    new LineTypeDefinition
                    {
                        Id = "ticks-reset",
                        Kind = LineTypeKind.VectorStamp,
                        Gap = 10d,
                        InitialGap = 0d,
                        StampSize = 8d,
                        OrientToTangent = true,
                        Perpendicular = true,
                        Flow = false
                    },
                    new SymbolDefinition
                    {
                        Id = "tick",
                        AnchorX = 4d,
                        AnchorY = 4d,
                        DefaultSize = 8d,
                        Primitives = new StylePrimitive[]
                        {
                            new StylePrimitive
                            {
                                Kind = SymbolPrimitiveKind.Polyline,
                                Coordinates = new[] { 0d, 4d, 8d, 4d },
                                StrokeColorArgb = unchecked((int)0xFF204060u),
                                FillColorArgb = 0,
                                StrokeWidth = 1d
                            }
                        }
                    });

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(10, 10).Alpha, Is.GreaterThan((byte)0));
                Assert.That(bitmap.GetPixel(10, 40).Alpha, Is.GreaterThan((byte)0));
            }
        }

        [Test]
        public void DrawPolyline_WithRasterPattern_RendersOffsetLanes()
        {
            using (var bitmap = new SKBitmap(96, 32, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var viewport = new IdentityViewport();
                var projector = new SkiaViewportProjector(viewport);
                projector.PrepareMatrix();

                var renderer = new SkiaLineStyleRenderer();
                renderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(8d, 16d),
                        new PhPoint(88d, 16d)
                    },
                    new LineTypeDefinition
                    {
                        Id = "double-track",
                        Kind = LineTypeKind.RasterPattern,
                        Width = 1d,
                        ColorArgb = unchecked((int)0xFF204060u),
                        Flow = true,
                        RasterPattern = new RasterLinePattern
                        {
                            Lanes = new[]
                            {
                                new RasterLinePatternLane
                                {
                                    OffsetY = -3d,
                                    RunLengths = new[] { 8, 4 },
                                    StartsWithDash = true
                                },
                                new RasterLinePatternLane
                                {
                                    OffsetY = 3d,
                                    RunLengths = new[] { 8, 4 },
                                    StartsWithDash = true
                                }
                            }
                        }
                    });

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(8, 13).Alpha, Is.GreaterThan((byte)0));
                Assert.That(bitmap.GetPixel(8, 19).Alpha, Is.GreaterThan((byte)0));
            }
        }

        [Test]
        public void DrawPolyline_WithRasterPatternAndFlowDisabled_RestartsPatternPerSegment()
        {
            using (var bitmap = new SKBitmap(80, 80, true))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                var viewport = new IdentityViewport();
                var projector = new SkiaViewportProjector(viewport);
                projector.PrepareMatrix();

                var renderer = new SkiaLineStyleRenderer();
                renderer.DrawPolyline(
                    canvas,
                    projector,
                    new[]
                    {
                        new PhPoint(10d, 10d),
                        new PhPoint(10d, 40d),
                        new PhPoint(40d, 40d)
                    },
                    new LineTypeDefinition
                    {
                        Id = "double-track-reset",
                        Kind = LineTypeKind.RasterPattern,
                        Width = 1d,
                        ColorArgb = unchecked((int)0xFF204060u),
                        Flow = false,
                        RasterPattern = new RasterLinePattern
                        {
                            Lanes = new[]
                            {
                                new RasterLinePatternLane
                                {
                                    OffsetY = 0d,
                                    RunLengths = new[] { 6, 4 },
                                    StartsWithDash = true
                                }
                            }
                        }
                    });

                projector.ReleaseMatrix();

                Assert.That(bitmap.GetPixel(10, 10).Alpha, Is.GreaterThan((byte)0));
                Assert.That(bitmap.GetPixel(10, 40).Alpha, Is.GreaterThan((byte)0));
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

