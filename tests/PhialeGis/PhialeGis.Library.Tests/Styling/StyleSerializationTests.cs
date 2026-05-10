using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Styling;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class StyleSerializationTests
    {
        [Test]
        public void SerializeDeserializeSymbols_RoundtripsPrimitives()
        {
            var catalog = new InMemorySymbolCatalog();
            catalog.Set(new SymbolDefinition
            {
                Id = "circle-symbol",
                Name = "Circle Symbol",
                AnchorX = 5d,
                AnchorY = 5d,
                DefaultSize = 10d,
                Primitives = new StylePrimitive[]
                {
                    new StylePrimitive
                    {
                        Kind = SymbolPrimitiveKind.Circle,
                        Coordinates = new[] { 5d, 5d, 4d },
                        StrokeColorArgb = unchecked((int)0xFF204060u),
                        FillColorArgb = unchecked((int)0xFFFFFFFFu),
                        StrokeWidth = 1.5d
                    }
                }
            });

            var serializer = new StyleCatalogSerializer();
            var json = serializer.SerializeSymbols(catalog);
            var roundtrip = serializer.DeserializeSymbols(json);

            var item = roundtrip.Single(x => x.Id == "circle-symbol");
            Assert.That(item.Primitives.Count, Is.EqualTo(1));
            Assert.That(item.Primitives[0].Kind, Is.EqualTo(SymbolPrimitiveKind.Circle));
            Assert.That(item.Primitives[0].Coordinates[2], Is.EqualTo(4d));
        }

        [Test]
        public void SerializeDeserializeLineTypes_RoundtripsVectorAndRasterDefinitions()
        {
            var catalog = new InMemoryLineTypeCatalog();
            catalog.Set(new LineTypeDefinition
            {
                Id = "vector-custom",
                Name = "Vector Custom",
                Kind = LineTypeKind.VectorStamp,
                SymbolId = "tick",
                StampSize = 9d,
                Gap = 13d,
                InitialGap = 2d,
                OrientToTangent = true,
                Perpendicular = true
            });
            catalog.Set(new LineTypeDefinition
            {
                Id = "raster-custom",
                Name = "Raster Custom",
                Kind = LineTypeKind.RasterPattern,
                RasterPattern = new RasterLinePattern
                {
                    Lanes = new[]
                    {
                        new RasterLinePatternLane
                        {
                            OffsetY = 1d,
                            RunLengths = new[] { 3, 2, 1 },
                            StartsWithDash = false
                        }
                    }
                }
            });

            var serializer = new StyleCatalogSerializer();
            var json = serializer.SerializeLineTypes(catalog);
            var roundtrip = serializer.DeserializeLineTypes(json);

            var vector = roundtrip.Single(x => x.Id == "vector-custom");
            var raster = roundtrip.Single(x => x.Id == "raster-custom");

            Assert.That(vector.SymbolId, Is.EqualTo("tick"));
            Assert.That(vector.Perpendicular, Is.True);
            Assert.That(raster.RasterPattern.Lanes[0].RunLengths, Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(raster.RasterPattern.Lanes[0].StartsWithDash, Is.False);
        }

        [Test]
        public void SerializeDeserializeFillStyles_RoundtripsTileBytes()
        {
            var catalog = new InMemoryFillStyleCatalog();
            catalog.Set(new FillStyleDefinition
            {
                Id = "tile-fill",
                Name = "Tile Fill",
                Kind = FillStyleKind.PatternTile,
                ForeColorArgb = unchecked((int)0xFF204060u),
                BackColorArgb = unchecked((int)0xFFFFFFFFu),
                TileWidth = 2,
                TileHeight = 2,
                TileBytes = new byte[] { 255, 0, 0, 255 }
            });

            var serializer = new StyleCatalogSerializer();
            var json = serializer.SerializeFillStyles(catalog);
            var roundtrip = serializer.DeserializeFillStyles(json);

            var tile = roundtrip.Single(x => x.Id == "tile-fill");
            Assert.That(tile.TileBytes, Is.EqualTo(new byte[] { 255, 0, 0, 255 }));
        }

        [Test]
        public void DeserializeSymbols_WithUnsupportedSchemaVersion_Throws()
        {
            var serializer = new StyleCatalogSerializer();
            var json = "{\"schemaVersion\":999,\"items\":[]}";

            Assert.That(() => serializer.DeserializeSymbols(json), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void StyleCatalogFileStore_SaveAndLoad_RoundtripsFiles()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "PhialeGis.StyleStoreTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var symbolCatalog = new InMemorySymbolCatalog();
                symbolCatalog.Set(new SymbolDefinition
                {
                    Id = "store-symbol",
                    Name = "Store Symbol",
                    DefaultSize = 8d,
                    Primitives = new StylePrimitive[]
                    {
                        new StylePrimitive
                        {
                            Kind = SymbolPrimitiveKind.Polygon,
                            Coordinates = new[] { 0d, 0d, 8d, 0d, 8d, 8d, 0d, 8d }
                        }
                    }
                });

                var fileStore = new StyleCatalogFileStore();
                var path = Path.Combine(tempRoot, "styles.symbols.json");
                fileStore.SaveSymbols(path, symbolCatalog);
                var roundtrip = fileStore.LoadSymbols(path);

                Assert.That(File.Exists(path), Is.True);
                Assert.That(roundtrip.Any(x => x.Id == "store-symbol"), Is.True);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

