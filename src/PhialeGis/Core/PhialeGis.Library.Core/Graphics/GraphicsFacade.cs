using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Core.Interactions;
using CoreGeometry = PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Render;
using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.Core.Systems;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Core.Styling;
using SpatialPrimitives = PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Systems;

namespace PhialeGis.Library.Core.Graphics
{
    /// <summary>
    /// Thin UI-facing facade that wires input, viewport and retained-mode rendering.
    /// </summary>
    internal class GraphicsFacade : IDisposable, IGraphicsFacade
    {
        private readonly ViewportManager _viewportManager;   
        private readonly DeviceDecorator _deviceDecorator;
        private readonly ManipulationHandler _manipulationHandler;
        private readonly InteractionMonitor _interactionMonitor;
        private readonly IDevice _device;
        private readonly IRedrawable _redrawable;
        private readonly System.Collections.Generic.HashSet<IEditorInteractive> _attachedEditors
    = new System.Collections.Generic.HashSet<IEditorInteractive>();


        // Retained-mode ECS + render system (no-op for now)
        private readonly IWorld _world = new SimpleWorld();

        private readonly ISymbolCatalog _symbolCatalog = new InMemorySymbolCatalog();
        private readonly ILineTypeCatalog _lineTypeCatalog = new InMemoryLineTypeCatalog();
        private readonly IFillStyleCatalog _fillStyleCatalog = new InMemoryFillStyleCatalog();
        private readonly StyleResolver _styleResolver;
        private readonly LineTypeDefinition _previewLineType;
        private readonly IPhRenderBackendFactory _backendFactory;
        private readonly IRenderSystem _render;

        public System.Action<PhialeGis.Library.Core.Scene.IWorld,
                     PhialeGis.Library.Abstractions.Ui.Rendering.IViewport> BeforeRender
        { get; set; }

        internal Func<double[]> PreviewProvider { get; set; }
        internal Func<SnapMarkerRenderState> SnapProvider { get; set; }


        public GraphicsFacade(IRenderingComposition renderingComposition, IPhRenderBackendFactory backendFactory)
        {
            if (renderingComposition == null) throw new ArgumentNullException(nameof(renderingComposition));
            if (backendFactory == null) throw new ArgumentNullException(nameof(backendFactory));

            _device = renderingComposition;
            _backendFactory = backendFactory;
            _deviceDecorator = new DeviceDecorator(renderingComposition);
            _styleResolver = new StyleResolver(_symbolCatalog, _lineTypeCatalog, _fillStyleCatalog);
            if (!_lineTypeCatalog.TryGet(BuiltInStyleIds.LineSolid, out _previewLineType))
                throw new InvalidOperationException($"Built-in line type '{BuiltInStyleIds.LineSolid}' is missing.");

            // ViewportManager implements IViewport and stays platform-agnostic
            _viewportManager = new ViewportManager(_deviceDecorator);

            // IUserInteractive may not be implemented by the composition -> guard for null
            var ui = renderingComposition as IUserInteractive;

            // Input handlers (they call OnRedrawRequested when needed)
            _manipulationHandler = new ManipulationHandler(_viewportManager, ui, OnRedrawRequested);
            _interactionMonitor = new InteractionMonitor(ui, _viewportManager, renderingComposition, OnRedrawRequested);
            
            
            _render = new PhGeometryCoreRenderSystem(backendFactory, _styleResolver);
            if (_render is PhialeGis.Library.Core.Render.PhGeometryCoreRenderSystem coreRender)
                coreRender.OverlayRenderer = RenderOverlays;
            // Draw wiring
            renderingComposition.PaintSurface += OnDraw;

            _redrawable = renderingComposition;
           
        }

        public void Invalidate()
        {
            // Triggers a redraw on the composition control
            _redrawable.Invalidate();
        }

        private void OnRedrawRequested(object sender, RedrawEventArgs e)
        {
            Invalidate();
        }

     

        /// <summary>
        /// Retained-mode render entry (ECS-based). Currently no-op renderer.
        /// </summary>
        public void OnDraw(object drawBox, IDisposable skCanvas)
        {
            if (!(skCanvas is IDisposable canvas)) return;

            // 1) Build render context (viewport is platform-agnostic; DPI comes from device)
            var ctx = new RenderContext(
                canvas: canvas,
                viewport: _viewportManager,           // ViewportManager implements IViewport
                dpiX: _viewportManager.GetDpiX(),
                dpiY: _viewportManager.GetDpiY()
            );

            // 2) Clear background (later: bind to theme)
           // canvas.Clear(SKColors.White);

            BeforeRender?.Invoke(_world, _viewportManager);

            // 3) Render ECS world (currently NoOp; will draw once symbolizers are added)
            _render.Render(_world, ctx);
        }

        

        /// <summary>
        /// Applies a new visible model window.
        /// NOTE: If PhRect ctor expects corners (x1,y1,x2,y2), pass x+width, y+height.
        /// </summary>
        public void ApplyVisualWindow(double x, double y, double width, double height)
        {
            // If your PhRect(x,y,w,h) is actually (x1,y1,x2,y2), convert here:
            var rect = new CoreGeometry.PhRect(x, y, x + width, y + height);  // <-- adjust if needed for your PhRect ctor
            _viewportManager.ApplyActiveView(rect);
            Invalidate();
        }

        /// <summary>
        /// Unwire events when disposing the facade.
        /// </summary>
        public void Dispose()
        {
            if (_device is IRenderingComposition rc)
            {
                rc.PaintSurface -= OnDraw;
            }

            // If you added a Detach() on ViewportManager to unsubscribe device events, call it here.
            // _viewportManager.Detach();
        }

        /// <summary>Exposes the rendering viewport used by this facade.</summary>
        internal IViewport Viewport
        {
            get { return _viewportManager; }
        }

        internal void AttachEditor(PhialeGis.Library.Abstractions.Interactions.IEditorInteractive editor)
        {
            if (editor == null) return;
            _attachedEditors.Add(editor);
        }

        internal void DetachEditor(PhialeGis.Library.Abstractions.Interactions.IEditorInteractive editor)
        {
            if (editor == null) return;
            _attachedEditors.Remove(editor);
        }

        
        internal System.Collections.Generic.IReadOnlyCollection<PhialeGis.Library.Abstractions.Interactions.IEditorInteractive> AttachedEditors
            => _attachedEditors;

        private void RenderOverlays(IPhRenderDriver backend, RenderContext ctx)
        {
            RenderPreview(backend, ctx);
            RenderSnapMarker(backend, ctx);
            RenderCursor(backend, ctx);
        }

        private void RenderPreview(IPhRenderDriver backend, RenderContext ctx)
        {
            var preview = PreviewProvider != null ? PreviewProvider() : null;
            if (preview == null || preview.Length < 4) return;

            var points = new List<SpatialPrimitives.PhPoint>(preview.Length / 2);
            for (int i = 0; i + 1 < preview.Length; i += 2)
                points.Add(new SpatialPrimitives.PhPoint(preview[i], preview[i + 1]));

            if (points.Count < 2) return;

            if (!(backend is IStyledOverlayRenderBackend overlayBackend))
                throw new NotSupportedException("Current render backend does not support styled overlay rendering.");

            overlayBackend.DrawOverlayStyledLine(new OverlayLineRenderRequest
            {
                Points = points,
                LineType = _previewLineType
            });
        }

        internal struct SnapMarkerRenderState
        {
            public bool HasSnap;
            public double ModelX;
            public double ModelY;
            public uint StrokeArgb;
            public float SizePx;
            public float ThicknessPx;
        }

        private void RenderSnapMarker(IPhRenderDriver backend, RenderContext ctx)
        {
            if (!(backend is PhialeGis.Library.Geometry.Systems.IPhScreenSpaceBackend screen))
                return;

            if (SnapProvider == null)
                return;

            var state = SnapProvider();
            if (!state.HasSnap)
                return;

            ctx.Viewport.ModelToScreen(state.ModelX, state.ModelY, out var x, out var y);

            var size = state.SizePx <= 0 ? 8f : state.SizePx;
            var half = size / 2f;
            var thickness = state.ThicknessPx <= 0 ? 1.5f : state.ThicknessPx;
            var color = state.StrokeArgb == 0 ? 0xFFFFA500 : state.StrokeArgb;

            screen.DrawScreenLine(x - half, y, x + half, y, color, thickness);
            screen.DrawScreenLine(x, y - half, x, y + half, color, thickness);
            screen.DrawScreenRect(x - half, y - half, size, size, color, thickness);
        }

        internal Func<CursorRenderState> CursorProvider { get; set; }

        internal struct CursorRenderState
        {
            public PhialeGis.Library.Abstractions.Actions.CursorSpec Spec;
            public double ScreenX;
            public double ScreenY;
            public bool HasPosition;
        }

        private void RenderCursor(IPhRenderDriver backend, RenderContext ctx)
        {
            if (!(backend is PhialeGis.Library.Geometry.Systems.IPhScreenSpaceBackend screen))
                return;

            if (CursorProvider == null) return;
            var state = CursorProvider();
            if (!state.HasPosition) return;
            if (state.Spec == null) return;

            var spec = state.Spec;
            // Keep cursor geometry in viewport/control coordinates.
            // Backend maps these to device pixels using its current canvas transform.
            var x = (float)state.ScreenX;
            var y = (float)state.ScreenY;

            var length = (float)spec.CrosshairLength;
            var gap = (float)spec.Gap;
            var aperture = (float)spec.ApertureSize;
            var thickness = (float)spec.Thickness;
            var color = spec.StrokeArgb;

            // Horizontal lines
            screen.DrawScreenLine(x - gap - length, y, x - gap, y, color, thickness);
            screen.DrawScreenLine(x + gap, y, x + gap + length, y, color, thickness);

            // Vertical lines
            screen.DrawScreenLine(x, y - gap - length, x, y - gap, color, thickness);
            screen.DrawScreenLine(x, y + gap, x, y + gap + length, color, thickness);

            // Aperture square (stroke)
            if (aperture > 0)
            {
                var half = aperture / 2f;
                screen.DrawScreenRect(x - half, y - half, aperture, aperture, color, thickness);
            }
        }
    }
}

