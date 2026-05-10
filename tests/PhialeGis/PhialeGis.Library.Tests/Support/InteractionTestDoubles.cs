using System;
using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Render;
using PhialeGis.Library.Geometry.Systems;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests.Support
{
    internal sealed class FakeRenderingComposition : IRenderingComposition, Abstractions.Interactions.IUserInteractive
    {
        public event EventHandler<object> ChangeVisualParams;
        public event EventHandler<IDisposable> PaintSurface;
        public event EventHandler<object> SurfaceShifted;
        public event EventHandler<UniversalPointerRoutedEventArgs> PointerPressedUniversal;
        public event EventHandler<UniversalPointerRoutedEventArgs> PointerMovedUniversal;
        public event EventHandler<UniversalPointerRoutedEventArgs> PointerEnteredUniversal;
        public event EventHandler<UniversalPointerRoutedEventArgs> PointerReleasedUniversal;
        public event EventHandler<UniversalManipulationStartingRoutedEventArgs> ManipulationStartingUniversal;
        public event EventHandler<UniversalManipulationStartedRoutedEventArgs> ManipulationStartedUniversal;
        public event EventHandler<UniversalManipulationDeltaRoutedEventArgs> ManipulationDeltaUniversal;
        public event EventHandler<UniversalManipulationCompletedRoutedEventArgs> ManipulationCompletedUniversal;

        public double CurrentWidth { get; set; } = 1000d;
        public double CurrentHeight { get; set; } = 1000d;

        public int InvalidateCount { get; private set; }
        public CursorType LastCursor { get; private set; } = CursorType.Arrow;

        public double GetDpiX() => 96d;

        public double GetDpiY() => 96d;

        public void Invalidate()
        {
            InvalidateCount++;
        }

        public void RaisePaint()
        {
            PaintSurface?.Invoke(this, new FakeCanvas());
        }

        public void RaiseVisualParamsChanged()
        {
            ChangeVisualParams?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseSurfaceShifted(double dx, double dy)
        {
            SurfaceShifted?.Invoke(this, new SurfaceMovement(dx, dy));
        }

        public void SetCursor(CursorType cursorType)
        {
            LastCursor = cursorType;
        }

        private sealed class FakeCanvas : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    internal sealed class CountingRenderBackendFactory : IPhRenderBackendFactory
    {
        public int OverlayLineDrawCount { get; private set; }
        public int ScreenLineDrawCount { get; private set; }
        public int ScreenRectDrawCount { get; private set; }

        public IPhRenderDriver Create(object canvas, IViewport viewport)
        {
            return new CountingRenderDriver(this);
        }

        private sealed class CountingRenderDriver : IPhRenderDriver, IPhRenderBackend, IStyledOverlayRenderBackend, IPhScreenSpaceBackend
        {
            private readonly CountingRenderBackendFactory _owner;

            public CountingRenderDriver(CountingRenderBackendFactory owner)
            {
                _owner = owner;
            }

            public void BeginUpdate()
            {
            }

            public void EndUpdate()
            {
            }

            public void DrawOverlayStyledLine(OverlayLineRenderRequest request)
            {
                _owner.OverlayLineDrawCount++;
            }

            public void DrawScreenLine(float x1, float y1, float x2, float y2, uint strokeArgb, float thicknessPx)
            {
                _owner.ScreenLineDrawCount++;
            }

            public void DrawScreenRect(float x, float y, float width, float height, uint strokeArgb, float thicknessPx)
            {
                _owner.ScreenRectDrawCount++;
            }
        }
    }
}

