using System;
using System.Globalization;
using System.Security.Cryptography;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Renderer.Skia.Styling;
using PhialeGis.Library.Renderer.Skia.ViewportProjections;
using SkiaSharp;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class SkiaStyleRenderingSnapshotTests
    {
        [Test]
        public void Render_DashLine_RuntimeHash_MatchesBaseline()
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
                        Id = "dash",
                        Kind = LineTypeKind.SimpleDash,
                        ColorArgb = unchecked((int)0xFF163046u),
                        Width = 2d,
                        DashPattern = new[] { 6d, 4d },
                        Flow = true
                    });

                projector.ReleaseMatrix();

                var actual = ComputePixelHash(bitmap);
                Assert.That(actual, Is.EqualTo("0969e23b9d481e986f5fce915d0b09b94912af2ce832bc7edcb524f7063526f7"), $"Actual hash: {actual}");
            }
        }

        [Test]
        public void Render_VectorStampLine_RuntimeHash_MatchesBaseline()
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
                    CreateTickSymbol());

                projector.ReleaseMatrix();

                var actual = ComputePixelHash(bitmap);
                Assert.That(actual, Is.EqualTo("f6be4476a0aa1689b2330567de6cef1269f5259e69dbf8bcbc3f108d03534974"), $"Actual hash: {actual}");
            }
        }

        [Test]
        public void Render_RasterPatternLine_RuntimeHash_MatchesBaseline()
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
                        ColorArgb = unchecked((int)0xFF163046u),
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

                var actual = ComputePixelHash(bitmap);
                Assert.That(actual, Is.EqualTo("0122e2dc7594217070ca8641f5374a79bcb5d929a9dfa45380d675d08fd82a9b"), $"Actual hash: {actual}");
            }
        }

        [Test]
        public void Render_PatternTileFill_RuntimeHash_MatchesBaseline()
        {
            using (var bitmap = new SKBitmap(40, 40, true))
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
                        new PhPoint(32d, 8d),
                        new PhPoint(32d, 32d),
                        new PhPoint(8d, 32d),
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

                var actual = ComputePixelHash(bitmap);
                Assert.That(actual, Is.EqualTo("f26279737f56d2a92baca908659b6c828b0938f9425d356e388093a86fcaf88d"), $"Actual hash: {actual}");
            }
        }

        [Test]
        public void Render_SymbolPreviewOnDarkBackground_HashMatchesBaseline()
        {
            var service = new SkiaStylePreviewService();
            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Symbol,
                WidthPx = 48,
                HeightPx = 48,
                BackgroundColorArgb = unchecked((int)0xFF0A1220u),
                Symbol = new SymbolDefinition
                {
                    Id = "dark-square",
                    AnchorX = 4d,
                    AnchorY = 4d,
                    DefaultSize = 8d,
                    Primitives = new StylePrimitive[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polygon,
                            Coordinates = new[] { 0d, 0d, 8d, 0d, 8d, 8d, 0d, 8d },
                            StrokeColorArgb = unchecked((int)0xFF0A1220u),
                            FillColorArgb = unchecked((int)0xFF0A1220u),
                            StrokeWidth = 1d
                        }
                    }
                }
            });

            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                var actual = ComputePixelHash(bitmap);
                Assert.That(actual, Is.EqualTo("17998d8577c4abc0f66ab3dda1be3b995cd6d8d63777ca68a7629b8121e52745"), $"Actual hash: {actual}");
            }
        }

        [Test]
        public void Render_FillPreviewGradientOnDarkBackground_HashMatchesBaseline()
        {
            var service = new SkiaStylePreviewService();
            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Fill,
                WidthPx = 64,
                HeightPx = 64,
                BackgroundColorArgb = unchecked((int)0xFF0A1220u),
                FillStyle = new FillStyleDefinition
                {
                    Id = "gradient",
                    Kind = FillStyleKind.Gradient,
                    ForeColorArgb = unchecked((int)0xFF163046u),
                    BackColorArgb = unchecked((int)0xFFFFFFFFu),
                    GradientDirection = GradientDirection.LeftToRight
                }
            });

            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                var actual = ComputePixelHash(bitmap);
                Assert.That(actual, Is.EqualTo("4f3014bc2de02d8b3469ed0d1a3e4fbc384f6d38c5c28df652dacb9e42548f1f"), $"Actual hash: {actual}");
            }
        }

        private static SymbolDefinition CreateTickSymbol()
        {
            return new SymbolDefinition
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
                        StrokeColorArgb = unchecked((int)0xFF163046u),
                        FillColorArgb = 0,
                        StrokeWidth = 1d
                    }
                }
            };
        }

        private static string ComputePixelHash(SKBitmap bitmap)
        {
            var buffer = new byte[bitmap.Width * bitmap.Height * 4];
            var index = 0;

            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    buffer[index++] = pixel.Red;
                    buffer[index++] = pixel.Green;
                    buffer[index++] = pixel.Blue;
                    buffer[index++] = pixel.Alpha;
                }
            }

            var hash = SHA256.HashData(buffer);
            return BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
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

