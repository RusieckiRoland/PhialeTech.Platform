using System;
using System.Linq;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Styling;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class StyleAuthoringServiceTests
    {
        [Test]
        public void CreateOrUpdateSymbol_StoresIndependentCopyInCatalog()
        {
            var symbolCatalog = new InMemorySymbolCatalog();
            var service = CreateService(symbolCatalog: symbolCatalog);
            var definition = new SymbolDefinition
            {
                Id = "custom-square",
                Name = "Custom Square",
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
            };

            var stored = service.CreateOrUpdateSymbol(definition);
            definition.Primitives[0].Coordinates[0] = 999d;

            Assert.That(symbolCatalog.TryGet("custom-square", out var fromCatalog), Is.True);
            Assert.That(fromCatalog.Primitives[0].Coordinates[0], Is.EqualTo(0d));
            Assert.That(stored.Primitives[0].Coordinates[0], Is.EqualTo(0d));
        }

        [Test]
        public void CreateVectorLineTypeFromSymbol_WithMissingSymbol_Throws()
        {
            var service = CreateService();

            Assert.That(
                () => service.CreateVectorLineTypeFromSymbol(
                    "missing-ref",
                    "Missing Ref",
                    "unknown-symbol",
                    unchecked((int)0xFF163046u),
                    1d,
                    8d,
                    12d),
                Throws.InvalidOperationException);
        }

        [Test]
        public void CreateVectorLineTypeFromSymbol_StoresResolvedDefinition()
        {
            var lineCatalog = new InMemoryLineTypeCatalog();
            var service = CreateService(lineTypeCatalog: lineCatalog);

            var lineType = service.CreateVectorLineTypeFromSymbol(
                "custom-tick",
                "Custom Tick",
                "tick",
                unchecked((int)0xFF204060u),
                1.5d,
                10d,
                14d,
                initialGap: 2d,
                flow: false,
                orientToTangent: true,
                perpendicular: true);

            Assert.That(lineType.Id, Is.EqualTo("custom-tick"));
            Assert.That(lineCatalog.TryGet("custom-tick", out var fromCatalog), Is.True);
            Assert.That(fromCatalog.SymbolId, Is.EqualTo("tick"));
            Assert.That(fromCatalog.Gap, Is.EqualTo(14d));
            Assert.That(fromCatalog.Flow, Is.False);
        }

        [Test]
        public void CreateRasterLineTypeFromBitmap_BuildsPatternAndStoresIt()
        {
            var lineCatalog = new InMemoryLineTypeCatalog();
            var service = CreateService(lineTypeCatalog: lineCatalog);
            var pixels = new[]
            {
                unchecked((int)0x00000000u), unchecked((int)0xFF000000u), unchecked((int)0x00000000u),
                unchecked((int)0xFF000000u), unchecked((int)0x00000000u), unchecked((int)0xFF000000u),
                unchecked((int)0x00000000u), unchecked((int)0xFF000000u), unchecked((int)0x00000000u)
            };

            var lineType = service.CreateRasterLineTypeFromBitmap(
                "bitmap-line",
                "Bitmap Line",
                3,
                3,
                pixels,
                unchecked((int)0xFF163046u),
                strokeWidth: 1d,
                flow: true,
                repeat: 6d);

            Assert.That(lineType.Kind, Is.EqualTo(LineTypeKind.RasterPattern));
            Assert.That(lineType.RasterPattern, Is.Not.Null);
            Assert.That(lineCatalog.TryGet("bitmap-line", out var fromCatalog), Is.True);
            Assert.That(fromCatalog.RasterPattern.Lanes.Count, Is.EqualTo(3));
        }

        [Test]
        public void CreateOrUpdateFillStyle_WithInvalidHatchSpacing_Throws()
        {
            var service = CreateService();

            Assert.That(
                () => service.CreateOrUpdateFillStyle(new FillStyleDefinition
                {
                    Id = "broken-hatch",
                    Kind = FillStyleKind.Hatch,
                    HatchSpacing = 0d,
                    HatchThickness = 1d
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void InMemoryCatalog_TryGet_ReturnsDetachedClone()
        {
            var catalog = new InMemorySymbolCatalog();

            Assert.That(catalog.TryGet(BuiltInStyleIds.SymbolSquare, out var first), Is.True);
            first.Primitives[0].Coordinates[0] = 777d;

            Assert.That(catalog.TryGet(BuiltInStyleIds.SymbolSquare, out var second), Is.True);
            Assert.That(second.Primitives[0].Coordinates[0], Is.EqualTo(0d));
        }

        private static StyleAuthoringService CreateService(
            IMutableSymbolCatalog symbolCatalog = null,
            IMutableLineTypeCatalog lineTypeCatalog = null,
            IMutableFillStyleCatalog fillStyleCatalog = null)
        {
            return new StyleAuthoringService(
                symbolCatalog ?? new InMemorySymbolCatalog(),
                lineTypeCatalog ?? new InMemoryLineTypeCatalog(),
                fillStyleCatalog ?? new InMemoryFillStyleCatalog());
        }
    }
}

