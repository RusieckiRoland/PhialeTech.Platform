using System.Linq;
using System;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Styling;
using PhialeGis.Library.Renderer.Skia.Styling;
using SkiaSharp;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class SkiaStylePreviewServiceTests
    {
        [Test]
        public void Render_LinePreview_ReturnsPngWithVisibleStroke()
        {
            var service = new SkiaStylePreviewService();

            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 96,
                HeightPx = 24,
                LineType = new LineTypeDefinition
                {
                    Id = "dash",
                    Kind = LineTypeKind.SimpleDash,
                    ColorArgb = unchecked((int)0xFF163046u),
                    Width = 2d,
                    DashPattern = new[] { 6d, 4d }
                }
            });

            Assert.That(image.PngBytes.Length, Is.GreaterThan(0));
            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                Assert.That(HasVisiblePixels(bitmap), Is.True);
            }
        }

        [Test]
        public void Render_VectorStampLinePreview_UsesResolvedStampSymbol()
        {
            var service = new SkiaStylePreviewService();

            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 96,
                HeightPx = 24,
                LineType = new LineTypeDefinition
                {
                    Id = "ticks-perp",
                    Kind = LineTypeKind.VectorStamp,
                    StampSize = 8d,
                    Gap = 12d,
                    OrientToTangent = true,
                    Perpendicular = true
                },
                LineStampSymbol = new SymbolDefinition
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
                }
            });

            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                Assert.That(HasVisiblePixels(bitmap), Is.True);
            }
        }

        [Test]
        public void Render_SymbolPreview_ReturnsPngWithVisiblePixels()
        {
            var service = new SkiaStylePreviewService();

            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Symbol,
                WidthPx = 48,
                HeightPx = 48,
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

            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                Assert.That(HasVisiblePixels(bitmap), Is.True);
            }
        }

        [Test]
        public void Render_FillPreviewGradient_ReturnsPngWithColorVariation()
        {
            var service = new SkiaStylePreviewService();

            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Fill,
                WidthPx = 64,
                HeightPx = 64,
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
                var left = bitmap.GetPixel(10, 32);
                var right = bitmap.GetPixel(54, 32);
                Assert.That(left, Is.Not.EqualTo(right));
            }
        }

        [Test]
        public void Render_LinePreviewWithoutLineType_Throws()
        {
            var service = new SkiaStylePreviewService();

            Assert.That(
                () => service.Render(new StylePreviewRequest
                {
                    Kind = StylePreviewKind.Line
                }),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Render_VectorStampLinePreviewWithoutStampSymbol_Throws()
        {
            var service = new SkiaStylePreviewService();

            Assert.That(
                () => service.Render(new StylePreviewRequest
                {
                    Kind = StylePreviewKind.Line,
                    LineType = new LineTypeDefinition
                    {
                        Id = "ticks-perp",
                        Kind = LineTypeKind.VectorStamp,
                        Gap = 12d,
                        StampSize = 8d
                    }
                }),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Render_LinePreview_OnDarkBackground_UsesHaloForLowContrastStroke()
        {
            var service = new SkiaStylePreviewService();
            var background = unchecked((int)0xFF0A1220u);

            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 96,
                HeightPx = 24,
                BackgroundColorArgb = background,
                LineType = new LineTypeDefinition
                {
                    Id = "night-line",
                    Kind = LineTypeKind.SimpleDash,
                    ColorArgb = background,
                    Width = 2d
                }
            });

            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                Assert.That(HasPixelsDifferentThan(bitmap, new SKColor(10, 18, 32, 255)), Is.True);
            }
        }

        [Test]
        public void Render_SymbolPreview_OnDarkBackground_UsesHaloForLowContrastSymbol()
        {
            var service = new SkiaStylePreviewService();
            var background = unchecked((int)0xFF0A1220u);

            var image = service.Render(new StylePreviewRequest
            {
                Kind = StylePreviewKind.Symbol,
                WidthPx = 48,
                HeightPx = 48,
                BackgroundColorArgb = background,
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
                            StrokeColorArgb = background,
                            FillColorArgb = background,
                            StrokeWidth = 1d
                        }
                    }
                }
            });

            using (var bitmap = SKBitmap.Decode(image.PngBytes))
            {
                Assert.That(HasPixelsDifferentThan(bitmap, new SKColor(10, 18, 32, 255)), Is.True);
            }
        }

        [Test]
        public void StylePreviewCache_BuildKey_ChangesWhenTileBytesChangeWithSameLength()
        {
            var first = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Fill,
                WidthPx = 64,
                HeightPx = 24,
                FillStyle = new FillStyleDefinition
                {
                    Id = "tile",
                    Kind = FillStyleKind.PatternTile,
                    TileWidth = 2,
                    TileHeight = 2,
                    TileBytes = new byte[] { 255, 0, 0, 255 }
                }
            };

            var second = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Fill,
                WidthPx = 64,
                HeightPx = 24,
                FillStyle = new FillStyleDefinition
                {
                    Id = "tile",
                    Kind = FillStyleKind.PatternTile,
                    TileWidth = 2,
                    TileHeight = 2,
                    TileBytes = new byte[] { 0, 255, 255, 0 }
                }
            };

            Assert.That(StylePreviewCache.BuildKey(first), Is.Not.EqualTo(StylePreviewCache.BuildKey(second)));
        }

        [Test]
        public void StylePreviewCache_BuildKey_ChangesWhenLineStampSymbolChanges()
        {
            var first = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 64,
                HeightPx = 24,
                LineType = new LineTypeDefinition
                {
                    Id = "ticks",
                    Kind = LineTypeKind.VectorStamp,
                    StampSize = 8d
                },
                LineStampSymbol = new SymbolDefinition
                {
                    Id = "tick-a",
                    AnchorX = 4d,
                    AnchorY = 4d,
                    DefaultSize = 8d,
                    Primitives = new StylePrimitive[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polyline,
                            Coordinates = new[] { 0d, 4d, 8d, 4d }
                        }
                    }
                }
            };

            var second = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 64,
                HeightPx = 24,
                LineType = new LineTypeDefinition
                {
                    Id = "ticks",
                    Kind = LineTypeKind.VectorStamp,
                    StampSize = 8d
                },
                LineStampSymbol = new SymbolDefinition
                {
                    Id = "tick-b",
                    AnchorX = 4d,
                    AnchorY = 4d,
                    DefaultSize = 8d,
                    Primitives = new StylePrimitive[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polyline,
                            Coordinates = new[] { 0d, 3d, 8d, 5d }
                        }
                    }
                }
            };

            Assert.That(StylePreviewCache.BuildKey(first), Is.Not.EqualTo(StylePreviewCache.BuildKey(second)));
        }

        [Test]
        public void StylePreviewCache_GetOrAdd_ReturnsSameInstanceForEquivalentRequest()
        {
            var cache = new StylePreviewCache();
            var request = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Fill,
                WidthPx = 32,
                HeightPx = 32,
                FillStyle = new FillStyleDefinition
                {
                    Id = "solid",
                    Kind = FillStyleKind.Solid,
                    ForeColorArgb = unchecked((int)0xFF204060u)
                }
            };

            var createdCount = 0;
            var first = cache.GetOrAdd(request, () =>
            {
                createdCount++;
                return new StylePreviewImage
                {
                    WidthPx = 32,
                    HeightPx = 32,
                    PngBytes = new byte[] { 1, 2, 3 }
                };
            });
            var second = cache.GetOrAdd(request, () =>
            {
                createdCount++;
                return new StylePreviewImage
                {
                    WidthPx = 32,
                    HeightPx = 32,
                    PngBytes = new byte[] { 4, 5, 6 }
                };
            });

            Assert.That(ReferenceEquals(first, second), Is.True);
            Assert.That(createdCount, Is.EqualTo(1));
        }

        [Test]
        public void StylePreviewCache_BuildKey_ChangesWhenPreviewDefinitionChanges()
        {
            var first = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 64,
                HeightPx = 24,
                LineType = new LineTypeDefinition
                {
                    Id = "dash",
                    Kind = LineTypeKind.SimpleDash,
                    DashPattern = new[] { 4d, 4d }
                },
                LineStampSymbol = new SymbolDefinition
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
                            Coordinates = new[] { 0d, 4d, 8d, 4d }
                        }
                    }
                }
            };

            var second = new StylePreviewRequest
            {
                Kind = StylePreviewKind.Line,
                WidthPx = 64,
                HeightPx = 24,
                LineType = new LineTypeDefinition
                {
                    Id = "dash",
                    Kind = LineTypeKind.SimpleDash,
                    DashPattern = new[] { 8d, 2d }
                },
                LineStampSymbol = new SymbolDefinition
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
                            Coordinates = new[] { 0d, 4d, 10d, 4d }
                        }
                    }
                }
            };

            Assert.That(StylePreviewCache.BuildKey(first), Is.Not.EqualTo(StylePreviewCache.BuildKey(second)));
        }

        private static bool HasVisiblePixels(SKBitmap bitmap)
        {
            return Enumerable.Range(0, bitmap.Height)
                .SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y)))
                .Any(pixel => pixel.Alpha > 0 && pixel != SKColors.White);
        }

        private static bool HasPixelsDifferentThan(SKBitmap bitmap, SKColor color)
        {
            return Enumerable.Range(0, bitmap.Height)
                .SelectMany(y => Enumerable.Range(0, bitmap.Width).Select(x => bitmap.GetPixel(x, y)))
                .Any(pixel => pixel != color);
        }
    }
}

