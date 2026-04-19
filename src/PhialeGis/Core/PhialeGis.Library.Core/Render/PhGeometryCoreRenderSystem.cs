using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.Geometry.Ecs;
using PhialeGis.Library.Geometry.Systems;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Systems;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Utils;
using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Styling;
using PhialeGis.Library.Geometry.Spatial.Multi;
using PhialeGis.Library.Geometry.Spatial.Single;
using System;

namespace PhialeGis.Library.Core.Render
{
    /// <summary>
    /// Core-level rendering system.
    /// Platform-agnostic: depends only on IPhRenderBackendFactory.
    /// </summary>
    public sealed class PhGeometryCoreRenderSystem : IRenderSystem
    {
        private readonly IPhRenderBackendFactory _backendFactory;
        private readonly StyleResolver _styleResolver;
        public Action<IPhRenderDriver, RenderContext> OverlayRenderer { get; set; }

        public PhGeometryCoreRenderSystem(IPhRenderBackendFactory backendFactory, StyleResolver styleResolver)
        {
            _backendFactory = backendFactory ?? throw new ArgumentNullException(nameof(backendFactory));
            _styleResolver = styleResolver ?? throw new ArgumentNullException(nameof(styleResolver));
        }

        public void Render(IWorld world, RenderContext ctx)
        {
            if (world == null || ctx?.Canvas == null || ctx.Viewport == null)
                return;

            // Backend (e.g. Skia, Win2D) provided by the platform layer.
            var backend = _backendFactory.Create(ctx.Canvas, ctx.Viewport);
            if (!(backend is IPhRenderBackend phRenderBackend))
                throw new InvalidOperationException("Render backend factory must create an IPhRenderBackend instance.");

            phRenderBackend.BeginUpdate();

            try
            {
                var entities = world.Entities;
                for (int i = 0; i < entities.Count; i++)
                {
                    var e = entities[i];

                    if (!world.TryGet(e, out PhGeometryComponent gc) || gc?.Geometry == null)
                        continue;

                    var geom = gc.LocalTransform.IsIdentity
                        ? gc.Geometry
                        : gc.Geometry.Bake(gc.LocalTransform);

                    if (!world.TryGet(e, out PhStyleComponent resolvedStyleReference))
                    {
                        throw new InvalidOperationException(
                            $"Renderable entity '{e}' does not provide a PhStyleComponent.");
                    }

                    var resolved = _styleResolver.Resolve(resolvedStyleReference);
                    RenderGeometry(backend, geom, resolved);
                }

                OverlayRenderer?.Invoke(backend, ctx);
            }
            finally
            {
                phRenderBackend.EndUpdate();
            }
        }

        private static void RenderGeometry(
            IPhRenderDriver backend,
            IPhGeometry geometry,
            ResolvedStyleSet resolved)
        {
            if (geometry == null)
                return;

            switch (geometry.Kind)
            {
                case PhGeometryKind.Point:
                    RenderPoint(backend, ((PhPointEntity)geometry).Point, resolved);
                    break;

                case PhGeometryKind.LineString:
                    RenderPolyline(backend, ((PhPolyLine)geometry).Points, resolved);
                    break;

                case PhGeometryKind.Polygon:
                    var polygon = (PhPolygon)geometry;
                    RenderPolygon(backend, polygon, resolved);
                    break;

                case PhGeometryKind.MultiPoint:
                    var multiPoint = (PhMultiPoint)geometry;
                    for (int i = 0; i < multiPoint.Points.Count; i++)
                        RenderPoint(backend, multiPoint.Points[i], resolved);
                    break;

                case PhGeometryKind.MultiLineString:
                    var multiLine = (PhMultiLineString)geometry;
                    for (int i = 0; i < multiLine.Lines.Count; i++)
                    {
                        RenderPolyline(backend, multiLine.Lines[i].Points, resolved);
                    }
                    break;

                case PhGeometryKind.MultiPolygon:
                    var multiPolygon = (PhMultiPolygon)geometry;
                    for (int i = 0; i < multiPolygon.Polygons.Count; i++)
                    {
                        var poly = multiPolygon.Polygons[i];
                        RenderPolygon(backend, poly, resolved);
                    }
                    break;

                case PhGeometryKind.GeometryCollection:
                    var collection = (PhGeometryCollection)geometry;
                    for (int i = 0; i < collection.Geometries.Count; i++)
                        RenderGeometry(backend, collection.Geometries[i], resolved);
                    break;
            }
        }

        private static void RenderPolyline(
            IPhRenderDriver backend,
            System.Collections.Generic.IList<Geometry.Spatial.Primitives.PhPoint> points,
            ResolvedStyleSet resolved)
        {
            if (!(backend is ILineStyleRenderDriver lineRenderDriver))
                throw new NotSupportedException("Current render backend does not support styled line rendering.");

            var request = new LineRenderRequest
            {
                Points = points,
                LineType = resolved.LineType,
                StampSymbol = resolved.LineStampSymbol
            };

            request.Validate();
            lineRenderDriver.DrawStyledLine(request);
        }

        private static void RenderPolygon(
            IPhRenderDriver backend,
            PhPolygon polygon,
            ResolvedStyleSet resolved)
        {
            if (!(backend is IFillStyleRenderDriver fillRenderDriver))
                throw new NotSupportedException("Current render backend does not support styled fill rendering.");

            var fillRequest = new FillRenderRequest
            {
                Outer = polygon.Outer,
                Holes = polygon.Holes,
                FillStyle = resolved.FillStyle
            };

            fillRequest.Validate();
            fillRenderDriver.FillPolygon(fillRequest);

            RenderPolyline(backend, polygon.Outer, resolved);

            if (polygon.Holes != null)
            {
                for (int i = 0; i < polygon.Holes.Count; i++)
                    RenderPolyline(backend, polygon.Holes[i], resolved);
            }
        }

        private static void RenderPoint(
            IPhRenderDriver backend,
            Geometry.Spatial.Primitives.PhPoint point,
            ResolvedStyleSet resolved)
        {
            if (resolved.Symbol == null)
                throw new InvalidOperationException("Point geometry requires SymbolId and a resolved symbol definition.");

            if (!(backend is ISymbolRenderDriver symbolRenderDriver))
                throw new NotSupportedException("Current render backend does not support symbol rendering.");

            symbolRenderDriver.DrawSymbol(new SymbolRenderRequest
            {
                ModelX = point.X,
                ModelY = point.Y,
                Symbol = resolved.Symbol,
                Size = resolved.Symbol.DefaultSize
            });
        }

        // === helpers =========================================================

        /// <summary>
        /// Checks whether the geometry intersects the current viewport clip rectangle.
        /// Uses PhRect (neutral Core geometry type).
        /// </summary>
        private static bool IntersectsScreen(IPhGeometry g, IViewport vp, PhRect clip)
        {
            if (!GeometryBounds.TryGet(g, out var minX, out var minY, out var maxX, out var maxY))
                return true; // draw if bounds unknown

            vp.ModelToScreen(minX, minY, out var sx1, out var sy1);
            vp.ModelToScreen(maxX, maxY, out var sx2, out var sy2);

            var geomRect = new PhRect(
                Math.Min(sx1, sx2),
                Math.Min(sy1, sy2),
                Math.Max(sx1, sx2),
                Math.Max(sy1, sy2)
            );

            // Inclusive intersection test
            return !(geomRect.X2 < clip.X1 ||
                     geomRect.X1 > clip.X2 ||
                     geomRect.Y2 < clip.Y1 ||
                     geomRect.Y1 > clip.Y2);
        }
    }
}

