using System;
using System.Collections.Generic;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Render;
using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.Core.Styling;
using PhialeGis.Library.Geometry.Ecs;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;
using PhialeGis.Library.Geometry.Systems;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class PhGeometryCoreRenderSystemStyleIntegrationTests
    {
        [Test]
        public void Render_WithCatalogLineTypeId_UsesResolvedStrokeColorAndWidth()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolyLine(new[]
                {
                    new PhPoint(0d, 0d),
                    new PhPoint(10d, 0d)
                }),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineDash,
                FillStyleId = BuiltInStyleIds.FillSolidWhite
            });

            system.Render(world, CreateContext());

            Assert.That(backend.StyledLineDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastLineRequest, Is.Not.Null);
            Assert.That(backend.LastLineRequest.LineType.Id, Is.EqualTo("dash"));
            Assert.That(backend.BeginCount, Is.EqualTo(1));
            Assert.That(backend.EndCount, Is.EqualTo(1));
        }

        [Test]
        public void Render_WithVectorStampLineType_PassesResolvedStampSymbolToStyledLineDriver()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolyLine(new[]
                {
                    new PhPoint(0d, 0d),
                    new PhPoint(20d, 0d)
                }),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineTicksPerpendicular,
                FillStyleId = BuiltInStyleIds.FillSolidWhite
            });

            system.Render(world, CreateContext());

            Assert.That(backend.StyledLineDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastLineRequest, Is.Not.Null);
            Assert.That(backend.LastLineRequest.LineType.Id, Is.EqualTo("ticks-perp"));
            Assert.That(backend.LastLineRequest.StampSymbol, Is.Not.Null);
            Assert.That(backend.LastLineRequest.StampSymbol.Id, Is.EqualTo("tick"));
        }

        [Test]
        public void Render_WithRasterPatternLineType_UsesStyledLineDriver()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolyLine(new[]
                {
                    new PhPoint(0d, 0d),
                    new PhPoint(20d, 0d)
                }),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineDoubleTrack,
                FillStyleId = BuiltInStyleIds.FillSolidWhite
            });

            system.Render(world, CreateContext());

            Assert.That(backend.StyledLineDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastLineRequest.LineType.Id, Is.EqualTo("double-track"));
            Assert.That(backend.LastLineRequest.StampSymbol, Is.Null);
        }

        [Test]
        public void Render_WithUnknownCatalogIds_FallsBackToCatalogDefaultsForKnownChannels()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolygon(
                    new[]
                    {
                        new PhPoint(0d, 0d),
                        new PhPoint(10d, 0d),
                        new PhPoint(10d, 10d),
                        new PhPoint(0d, 0d)
                    }),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = "missing-line",
                FillStyleId = "missing-fill"
            });

            Assert.That(
                () => system.Render(world, CreateContext()),
                Throws.TypeOf<KeyNotFoundException>().With.Message.Contains("missing-line"));
        }

        [Test]
        public void Render_PolygonWithCatalogFill_UsesStyledFillDriver()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolygon(
                    new[]
                    {
                        new PhPoint(0d, 0d),
                        new PhPoint(5d, 0d),
                        new PhPoint(5d, 5d),
                        new PhPoint(0d, 0d)
                    }),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineSolid,
                FillStyleId = BuiltInStyleIds.FillHatch45
            });

            system.Render(world, CreateContext());

            Assert.That(backend.StyledFillDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastFillRequest, Is.Not.Null);
            Assert.That(backend.LastFillRequest.FillStyle.Id, Is.EqualTo("hatch-45"));
            Assert.That(backend.StyledLineDrawCount, Is.EqualTo(1));
        }

        [Test]
        public void Render_PolygonWithCatalogLineStyle_RendersFillAndStyledOutline()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolygon(
                    new[]
                    {
                        new PhPoint(0d, 0d),
                        new PhPoint(8d, 0d),
                        new PhPoint(8d, 8d),
                        new PhPoint(0d, 0d)
                    }),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineDash,
                FillStyleId = BuiltInStyleIds.FillSolidWhite
            });

            system.Render(world, CreateContext());

            Assert.That(backend.StyledFillDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastFillRequest.FillStyle.Id, Is.EqualTo("solid-white"));
            Assert.That(backend.StyledLineDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastLineRequest.LineType.Id, Is.EqualTo("dash"));
        }

        [Test]
        public void Render_WithoutStyleComponent_Throws()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPolyLine(new[]
                {
                    new PhPoint(0d, 0d),
                    new PhPoint(1d, 1d)
                }),
                LocalTransform = PhMatrix2D.Identity
            });

            Assert.That(
                () => system.Render(world, CreateContext()),
                Throws.InvalidOperationException.With.Message.Contains("PhStyleComponent"));
        }

        [Test]
        public void Render_PointWithSymbolId_UsesSymbolRenderDriverInsteadOfPrimitivePoint()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPointEntity(new PhPoint(12d, 34d)),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineSolid,
                FillStyleId = BuiltInStyleIds.FillSolidWhite,
                SymbolId = BuiltInStyleIds.SymbolTriangle
            });

            system.Render(world, CreateContext());

            Assert.That(backend.SymbolDrawCount, Is.EqualTo(1));
            Assert.That(backend.LastSymbolRequest, Is.Not.Null);
            Assert.That(backend.LastSymbolRequest.Symbol.Id, Is.EqualTo("triangle"));
            Assert.That(backend.LastSymbolRequest.ModelX, Is.EqualTo(12d));
            Assert.That(backend.LastSymbolRequest.ModelY, Is.EqualTo(34d));
        }

        [Test]
        public void Render_PointWithoutSymbolId_Throws()
        {
            var backend = new FakeRenderBackend();
            var system = CreateSystem(backend);
            var world = new SimpleWorld();
            var entity = world.Create();

            world.Add(entity, new PhGeometryComponent
            {
                Geometry = new PhPointEntity(new PhPoint(12d, 34d)),
                LocalTransform = PhMatrix2D.Identity
            });
            world.Add(entity, new PhStyleComponent
            {
                LineTypeId = BuiltInStyleIds.LineSolid,
                FillStyleId = BuiltInStyleIds.FillSolidWhite
            });

            Assert.That(
                () => system.Render(world, CreateContext()),
                Throws.InvalidOperationException.With.Message.Contains("Point geometry requires SymbolId"));
        }

        private static PhGeometryCoreRenderSystem CreateSystem(FakeRenderBackend backend)
        {
            return new PhGeometryCoreRenderSystem(
                new FakeBackendFactory(backend),
                new StyleResolver(
                    new InMemorySymbolCatalog(),
                    new InMemoryLineTypeCatalog(),
                    new InMemoryFillStyleCatalog()));
        }

        private static RenderContext CreateContext()
        {
            return new RenderContext(new FakeCanvas(), new FakeViewport(), 96d, 96d);
        }

        private sealed class FakeCanvas : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private sealed class FakeViewport : IViewport
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

        private sealed class FakeBackendFactory : IPhRenderBackendFactory
        {
            private readonly FakeRenderBackend _backend;

            public FakeBackendFactory(FakeRenderBackend backend)
            {
                _backend = backend;
            }

            public IPhRenderDriver Create(object canvas, IViewport viewport)
            {
                return _backend;
            }
        }

        private sealed class FakeRenderBackend : IPhRenderBackend, IPhRenderDriver, ISymbolRenderDriver, ILineStyleRenderDriver, IFillStyleRenderDriver
        {
            public int BeginCount { get; private set; }

            public int EndCount { get; private set; }

            public int SymbolDrawCount { get; private set; }

            public int StyledLineDrawCount { get; private set; }

            public int StyledFillDrawCount { get; private set; }

            public SymbolRenderRequest LastSymbolRequest { get; private set; }

            public LineRenderRequest LastLineRequest { get; private set; }

            public FillRenderRequest LastFillRequest { get; private set; }

            public void DrawStyledLine(LineRenderRequest request)
            {
                StyledLineDrawCount++;
                LastLineRequest = request;
            }

            public void FillPolygon(FillRenderRequest request)
            {
                StyledFillDrawCount++;
                LastFillRequest = request;
            }

            public void DrawSymbol(SymbolRenderRequest request)
            {
                SymbolDrawCount++;
                LastSymbolRequest = request;
            }

            public void BeginUpdate()
            {
                BeginCount++;
            }

            public void EndUpdate()
            {
                EndCount++;
            }
        }
    }
}

