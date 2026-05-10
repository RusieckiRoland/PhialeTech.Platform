using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Styling;
using PhialeGis.Library.Geometry.Ecs;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class StyleResolverTests
    {
        [Test]
        public void Resolve_WithNullStyle_Throws()
        {
            var resolver = CreateResolver();

            Assert.That(() => resolver.Resolve(null), Throws.InvalidOperationException);
        }

        [Test]
        public void Resolve_WithMissingCatalogIds_Throws()
        {
            var resolver = CreateResolver();
            Assert.That(
                () => resolver.Resolve(new PhStyleComponent()),
                Throws.InvalidOperationException.With.Message.Contains("LineTypeId"));
        }

        [Test]
        public void Resolve_WithKnownCatalogIds_UsesReferencedDefinitions()
        {
            var resolver = CreateResolver();
            var style = new PhStyleComponent
            {
                LineTypeId = "dash",
                FillStyleId = "hatch-45",
                SymbolId = "triangle"
            };

            var resolved = resolver.Resolve(style);

            Assert.That(resolved.LineType.Id, Is.EqualTo("dash"));
            Assert.That(resolved.LineType.DashPattern, Is.EqualTo(new[] { 8d, 6d }));
            Assert.That(resolved.FillStyle.Id, Is.EqualTo("hatch-45"));
            Assert.That(resolved.FillStyle.Kind, Is.EqualTo(FillStyleKind.Hatch));
            Assert.That(resolved.Symbol.Id, Is.EqualTo("triangle"));
        }

        [Test]
        public void Resolve_WithVectorStampLine_ResolvesStampSymbolFromCatalog()
        {
            var resolver = CreateResolver();
            var style = new PhStyleComponent
            {
                LineTypeId = "ticks-perp",
                FillStyleId = BuiltInStyleIds.FillSolidWhite
            };

            var resolved = resolver.Resolve(style);

            Assert.That(resolved.LineType.Id, Is.EqualTo("ticks-perp"));
            Assert.That(resolved.LineStampSymbol, Is.Not.Null);
            Assert.That(resolved.LineStampSymbol.Id, Is.EqualTo("tick"));
        }

        [Test]
        public void Resolve_WithUnknownCatalogIds_Throws()
        {
            var resolver = CreateResolver();
            var style = new PhStyleComponent
            {
                LineTypeId = "missing-line",
                FillStyleId = "missing-fill",
                SymbolId = "missing-symbol"
            };

            Assert.That(
                () => resolver.Resolve(style),
                Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("missing-line"));
        }

        [Test]
        public void Resolve_WithUnknownSymbolId_Throws()
        {
            var resolver = CreateResolver();
            var style = new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineSolid,
                FillStyleId = BuiltInStyleIds.FillSolidWhite,
                SymbolId = "missing-symbol"
            };

            Assert.That(
                () => resolver.Resolve(style),
                Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("missing-symbol"));
        }

        [Test]
        public void Resolve_WithVectorStampLineReferencingMissingSymbol_Throws()
        {
            var lineCatalog = new InMemoryLineTypeCatalog();
            lineCatalog.Set(new LineTypeDefinition
            {
                Id = "broken-stamp",
                Name = "Broken Stamp",
                Kind = LineTypeKind.VectorStamp,
                SymbolId = "missing-symbol",
                StampSize = 8d,
                Gap = 10d
            });

            var resolver = new StyleResolver(
                new InMemorySymbolCatalog(),
                lineCatalog,
                new InMemoryFillStyleCatalog());

            Assert.That(
                () => resolver.Resolve(new PhStyleComponent
                {
                    LineTypeId = "broken-stamp",
                    FillStyleId = BuiltInStyleIds.FillSolidWhite
                }),
                Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("missing-symbol"));
        }

        [Test]
        public void InMemoryCatalogs_ExposeRequiredDefaults()
        {
            var lineCatalog = new InMemoryLineTypeCatalog();
            var fillCatalog = new InMemoryFillStyleCatalog();
            var symbolCatalog = new InMemorySymbolCatalog();

            Assert.That(lineCatalog.GetAll().Select(x => x.Id), Does.Contain("solid"));
            Assert.That(lineCatalog.GetAll().Select(x => x.Id), Does.Contain("dash"));
            Assert.That(lineCatalog.GetAll().Select(x => x.Id), Does.Contain("ticks-perp"));
            Assert.That(lineCatalog.GetAll().Select(x => x.Id), Does.Contain("double-track"));
            Assert.That(fillCatalog.GetAll().Select(x => x.Id), Does.Contain("solid-white"));
            Assert.That(fillCatalog.GetAll().Select(x => x.Id), Does.Contain("hatch-45"));
            Assert.That(symbolCatalog.GetAll().Select(x => x.Id), Does.Contain("square"));
            Assert.That(symbolCatalog.GetAll().Select(x => x.Id), Does.Contain("triangle"));
            Assert.That(symbolCatalog.GetAll().Select(x => x.Id), Does.Contain("tick"));
        }

        private static StyleResolver CreateResolver()
        {
            return new StyleResolver(
                new InMemorySymbolCatalog(),
                new InMemoryLineTypeCatalog(),
                new InMemoryFillStyleCatalog());
        }
    }
}

