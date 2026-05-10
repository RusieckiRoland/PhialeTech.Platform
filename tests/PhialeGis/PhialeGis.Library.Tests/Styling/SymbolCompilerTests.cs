using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Renderer.Skia.Styling;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class SymbolCompilerTests
    {
        [Test]
        public void Compile_WithPolygonAndCircle_PrimitivesBuildsPictureAndBounds()
        {
            var compiler = new SymbolCompiler();
            var definition = new SymbolDefinition
            {
                Id = "sample",
                Name = "Sample",
                AnchorX = 0.5d,
                AnchorY = 0.5d,
                DefaultSize = 12d,
                Primitives = new StylePrimitive[]
                {
                    new StylePrimitive
                    {
                        Kind = SymbolPrimitiveKind.Polygon,
                        Coordinates = new[] { 0d, 0d, 10d, 0d, 10d, 10d, 0d, 10d },
                        StrokeColorArgb = unchecked((int)0xFF102030u),
                        FillColorArgb = unchecked((int)0x80A0B0C0u),
                        StrokeWidth = 2d
                    },
                    new StylePrimitive
                    {
                        Kind = SymbolPrimitiveKind.Circle,
                        Coordinates = new[] { 5d, 5d, 2d },
                        StrokeColorArgb = unchecked((int)0xFF405060u),
                        FillColorArgb = unchecked((int)0xFFFFFFFFu),
                        StrokeWidth = 1d
                    }
                }
            };

            using (var compiled = compiler.Compile(definition))
            {
                Assert.That(compiled.SymbolId, Is.EqualTo("sample"));
                Assert.That(compiled.Picture, Is.Not.Null);
                Assert.That(compiled.Primitives.Count, Is.EqualTo(2));
                Assert.That(compiled.Bounds.Left, Is.LessThanOrEqualTo(-1f));
                Assert.That(compiled.Bounds.Top, Is.LessThanOrEqualTo(-1f));
                Assert.That(compiled.Bounds.Right, Is.GreaterThanOrEqualTo(11f));
                Assert.That(compiled.Bounds.Bottom, Is.GreaterThanOrEqualTo(11f));
            }
        }

        [Test]
        public void Compile_WithInvalidPolylineCoordinates_ThrowsArgumentException()
        {
            var compiler = new SymbolCompiler();
            var definition = new SymbolDefinition
            {
                Id = "broken",
                Primitives = new StylePrimitive[]
                {
                    new StylePrimitive
                    {
                        Kind = SymbolPrimitiveKind.Polyline,
                        Coordinates = new[] { 0d, 0d, 1d }
                    }
                }
            };

            Assert.That(() => compiler.Compile(definition), Throws.ArgumentException);
        }

        [Test]
        public void ComputeContentHash_ChangesWhenPrimitiveGeometryChanges()
        {
            var compiler = new SymbolCompiler();
            var definition = new SymbolDefinition
            {
                Id = "triangle",
                Primitives = new StylePrimitive[]
                {
                    new StylePrimitive
                    {
                        Kind = SymbolPrimitiveKind.Polygon,
                        Coordinates = new[] { 0d, 0d, 1d, 0d, 0d, 1d }
                    }
                }
            };

            var hashBefore = compiler.ComputeContentHash(definition);
            definition.Primitives[0].Coordinates = new[] { 0d, 0d, 2d, 0d, 0d, 2d };
            var hashAfter = compiler.ComputeContentHash(definition);

            Assert.That(hashAfter, Is.Not.EqualTo(hashBefore));
        }

        [Test]
        public void SymbolCache_GetOrAdd_ReusesCompiledSymbolForSameDefinition()
        {
            using (var cache = new SymbolCache())
            {
                var definition = CreateSquareDefinition();

                var first = cache.GetOrAdd(definition);
                var second = cache.GetOrAdd(definition);

                Assert.That(ReferenceEquals(first, second), Is.True);
            }
        }

        [Test]
        public void SymbolCache_GetOrAdd_ReplacesCachedVersionWhenDefinitionChanges()
        {
            using (var cache = new SymbolCache())
            {
                var definition = CreateSquareDefinition();
                var first = cache.GetOrAdd(definition);

                definition.Primitives[0].Coordinates = new[] { 0d, 0d, 2d, 0d, 2d, 2d, 0d, 2d };
                var second = cache.GetOrAdd(definition);

                Assert.That(ReferenceEquals(first, second), Is.False);
                Assert.That(cache.TryGet(definition, out var cached), Is.True);
                Assert.That(ReferenceEquals(second, cached), Is.True);
            }
        }

        private static SymbolDefinition CreateSquareDefinition()
        {
            return new SymbolDefinition
            {
                Id = "square",
                Name = "Square",
                AnchorX = 0.5d,
                AnchorY = 0.5d,
                DefaultSize = 8d,
                Primitives = new StylePrimitive[]
                {
                    new StylePrimitive
                    {
                        Kind = SymbolPrimitiveKind.Polygon,
                        Coordinates = new[] { 0d, 0d, 1d, 0d, 1d, 1d, 0d, 1d },
                        StrokeColorArgb = unchecked((int)0xFF163046u),
                        FillColorArgb = unchecked((int)0xFFFFFFFFu),
                        StrokeWidth = 0.2d
                    }
                }
            };
        }
    }
}

