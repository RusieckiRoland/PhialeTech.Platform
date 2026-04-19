//PhialeDrawBoxUwp.cs
using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using SkiaSharp.Views.UWP;
using System;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace PhialeGis.Library.UwpUi.Controls
{
    /// <summary>
    /// UWP drawing host control backed by SkiaSharp.
    /// Uses composition for <see cref="IRedrawable"/> via an internal adapter.
    /// </summary>
    [TemplatePart(Name = "ScrollBarVertical", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "ScrollBarHorizontal", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "SkiaCanvas", Type = typeof(SKXamlCanvas))]
    public sealed partial class PhialeDrawBoxUwp : Control
    {
        #region Private Fields

        private ScrollBar _scrollBarVertical;
        private ScrollBar _scrollBarHorizontal;

        private double _dpiX;
        private double _dpiY;

        private double _lastScrolledValueX = 0;
        private double _lastScrolledValueY = 0;

        private SKXamlCanvas _skiaCanvas;

        #endregion Private Fields

        #region Composition 

        /// <summary>
        /// Internal adapter that provides <see cref="IRedrawable"/> without
        /// exposing non-WinRT interfaces on the control itself.
        /// </summary>
        internal sealed partial class RedrawableAdapter : IRenderingComposition
        {
            private readonly PhialeDrawBoxUwp _owner;
            public RedrawableAdapter(PhialeDrawBoxUwp owner) => _owner = owner;

            public double CurrentWidth => _owner.CurrentWidth;
            public double CurrentHeight => _owner.CurrentHeight;
            

            public event EventHandler<object> ChangeVisualParams;
            public event EventHandler<IDisposable> PaintSurface;

            public double GetDpiX()
            {
                return _owner.GetDpiX();
            }

            public double GetDpiY()
            {
                return _owner.GetDpiY();
            }

            /// <summary>
            /// Requests a redraw of the underlying Skia canvas.
            /// Safe to call from the UI thread.
            /// </summary>
            public void Invalidate() => _owner._skiaCanvas?.Invalidate();

            public void SetCursor(CursorType cursorType)
            {
                _owner.SetCursor(cursorType);
            }
            
            /// <summary>
            /// Raises <see cref="ChangeVisualParams"/>.
            /// </summary>
            public void OnVisualParamsChanged()
            {
                var handler = ChangeVisualParams;
                handler?.Invoke(this, EventArgs.Empty);
                _owner.RaiseChangeVisualParams();
            }

            /// <summary>
            /// Forwards Skia paint requests to subscribers.
            /// </summary>
            public void DoPaintSurface(object sender, SKPaintSurfaceEventArgs e)
            {
                var handler = PaintSurface;
                var canvas = e.Surface.Canvas;
                handler?.Invoke(this, canvas);
                _owner.RaisePaintSurface(canvas);
            }
        }

        /// <summary>
        /// Internal composition entry point for redraw operations.
        /// Use <c>Redrawable.Invalidate()</c> instead of calling the control directly.
        /// </summary>
        internal RedrawableAdapter Redrawable { get; }

        /// <summary>
        /// Optional generic exposure of the adapter (API symmetry with UWP).
        /// </summary>
        public object CompositionAdapter => Redrawable;

        #endregion Composition (IRedrawable)

        #region Properties

        /// <summary>
        /// Current width of the Skia canvas in device-independent pixels.
        /// </summary>
        public double CurrentWidth => _skiaCanvas != null ? _skiaCanvas.ActualWidth : 0;

        /// <summary>
        /// Current height of the Skia canvas in device-independent pixels.
        /// </summary>
        public double CurrentHeight => _skiaCanvas != null ? _skiaCanvas.ActualHeight : 0;

        /// <summary>
        /// Current horizontal DPI of the display.
        /// </summary>
        public double GetDpiX() => _dpiX;

        /// <summary>
        /// Current vertical DPI of the display.
        /// </summary>
        public double GetDpiY() => _dpiY;

        /// <summary>
        /// Default manipulation flags applied to the control.
        /// </summary>
        public ManipulationModes DefaultManipulationMode { get; set; } = ManipulationModes.All;
        

        #endregion Properties

        #region Events

        /// <summary>
        /// Raised when visual parameters change (size/DPI).
        /// </summary>
        public event EventHandler<object> ChangeVisualParams;

        /// <summary>
        /// Raised when the Skia surface is ready to be painted.
        /// The <see cref="IDisposable"/> argument is the Skia canvas surface.
        /// </summary>
        public event EventHandler<IDisposable> PaintSurface;

        

        #endregion Events

        #region Initialization

        /// <summary>
        /// Initializes a new instance of <see cref="PhialeDrawBoxUwp"/>.
        /// </summary>
        public PhialeDrawBoxUwp()
        {
            this.DefaultStyleKey = typeof(PhialeDrawBoxUwp);

            // Composition instead of inheriting IRedrawable
            Redrawable = new RedrawableAdapter(this);

            InitializeDpi();
            DisplayInformation.DisplayContentsInvalidated += DoDpiChanged;

            this.ManipulationMode = ManipulationModes.TranslateX |
                                    ManipulationModes.TranslateY |
                                    ManipulationModes.Scale |
                                    ManipulationModes.Rotate;

            this.SizeChanged += DoSizeChanged;
        }

        #endregion Initialization

        #region Utility Methods

        /// <summary>
        /// Reads current DPI from the active display.
        /// </summary>
        private void InitializeDpi()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            _dpiX = displayInformation.LogicalDpi;
            _dpiY = displayInformation.LogicalDpi;
        }

        /// <summary>
        /// Convenience wrapper for legacy call sites.
        /// Prefer <c>Redrawable.Invalidate()</c>.
        /// </summary>
        internal void Invalidate() => Redrawable.Invalidate();

        private void RaiseChangeVisualParams()
        {
            var handler = ChangeVisualParams;
            handler?.Invoke(this, EventArgs.Empty);
        }

        private void RaisePaintSurface(IDisposable canvas)
        {
            var handler = PaintSurface;
            handler?.Invoke(this, canvas);
        }

        /// <summary>
        /// Sets the core pointer cursor according to the requested cursor type.
        /// </summary>
        internal void SetCursor(PhialeGis.Library.Abstractions.Ui.Enums.CursorType cursorType)
        {
            var coreWindow = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            coreWindow.PointerCursor = cursorType == CursorType.Hand
                ? new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 0)
                : new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
        }

        #endregion Utility Methods

        #region Event Handlers        

        /// <summary>
        /// Handles horizontal scrollbar movement and emits logical surface translation.
        /// </summary>
        private void DoHorizontalScrollBarScroll(object sender, ScrollEventArgs e)
        {
            double offsetX = e.NewValue - _lastScrolledValueX;
            var translationX = offsetX * this.ActualWidth;
            _lastScrolledValueX = e.NewValue;
            var surfaceMovement = new SurfaceMovement(translationX, 0);
            Redrawable.RaiseSurfaceShifted(translationX, 0);
        }

        /// <summary>
        /// Handles vertical scrollbar movement and emits logical surface translation.
        /// </summary>
        private void DoVerticalScrollBarScroll(object sender, ScrollEventArgs e)
        {
            double offsetY = _lastScrolledValueY - e.NewValue;
            var translationY = offsetY * this.ActualHeight;
            _lastScrolledValueY = e.NewValue;
            var surfaceMovement = new SurfaceMovement(0, translationY);
            Redrawable.RaiseSurfaceShifted(0, translationY);
        }

        /// <summary>
        /// Updates DPI when display information changes.
        /// </summary>
        private void DoDpiChanged(DisplayInformation sender, object args)
        {
            _dpiX = sender.LogicalDpi;
            _dpiY = sender.LogicalDpi;
            Redrawable.OnVisualParamsChanged();
        }

        /// <summary>
        /// Propagates size changes to listeners.
        /// </summary>
        private void DoSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redrawable.OnVisualParamsChanged();
        }

        #endregion Event Handlers

        #region Overrides

        /// <summary>
        /// Resolves template parts and wires up event handlers.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollBarVertical = GetTemplateChild("ScrollBarVertical") as ScrollBar;
            _scrollBarHorizontal = GetTemplateChild("ScrollBarHorizontal") as ScrollBar;

            if (_scrollBarVertical != null)
                _scrollBarVertical.Scroll += DoVerticalScrollBarScroll;

            if (_scrollBarHorizontal != null)
                _scrollBarHorizontal.Scroll += DoHorizontalScrollBarScroll;

            _skiaCanvas = GetTemplateChild("SkiaCanvas") as SKXamlCanvas;
            if (_skiaCanvas != null)
                _skiaCanvas.PaintSurface += Redrawable.DoPaintSurface;
        }

        #endregion Overrides

        
    }
}
