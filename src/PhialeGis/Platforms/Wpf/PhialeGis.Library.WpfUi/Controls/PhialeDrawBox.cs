//PhialeDrawBox.cs
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace PhialeGis.Library.WpfUi.Controls
{
    /// <summary>
    /// WPF drawing host control backed by SkiaSharp.
    /// Uses composition for <see cref="IRenderingComposition"/> via an internal adapter.
    /// </summary>
    /// <remarks>
    /// Template parts mirror the UWP control for parity:
    /// "ScrollBarVertical", "ScrollBarHorizontal", "SkiaCanvas".
    /// </remarks>
    // Keep TemplatePart names consistent with UWP: ScrollBarVertical, ScrollBarHorizontal, SkiaCanvas
    [TemplatePart(Name = "ScrollBarVertical", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "ScrollBarHorizontal", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "SkiaCanvas", Type = typeof(SKElement))]
    [TemplatePart(Name = "PART_StatusPrimaryText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_StatusCoordinateText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_StatusSnapText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_TakeoverButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_UndoButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_FinishButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_CancelButton", Type = typeof(Button))]
    public partial class PhialeDrawBox : Control
    {
        #region Private Fields

        private ScrollBar? _scrollBarVertical;
        private ScrollBar? _scrollBarHorizontal;

        private double _dpiX;
        private double _dpiY;

        private double _lastScrolledValueX = 0;
        private double _lastScrolledValueY = 0;

        private SKElement? _skiaCanvas;
        private TextBlock? _statusPrimaryText;
        private TextBlock? _statusCoordinateText;
        private TextBlock? _statusSnapText;
        private Button? _takeoverButton;
        private Button? _undoButton;
        private Button? _finishButton;
        private Button? _cancelButton;

        #endregion Private Fields

        #region Composition

        /// <summary>
        /// Internal adapter that provides <see cref="IRenderingComposition"/> via composition.
        /// This avoids exposing non-WPF interfaces on the control itself (symmetry with UWP).
        /// </summary>
        internal sealed partial class RedrawableAdapter : IRenderingComposition
        {
            private readonly PhialeDrawBox _owner;
            public RedrawableAdapter(PhialeDrawBox owner) => _owner = owner;

            /// <summary>
            /// Current width of the drawing surface in device-independent pixels.
            /// </summary>
            public double CurrentWidth => _owner.CurrentWidth;

            /// <summary>
            /// Current height of the drawing surface in device-independent pixels.
            /// </summary>
            public double CurrentHeight => _owner.CurrentHeight;

            /// <summary>
            /// Raised when visual parameters change (size/DPI).
            /// </summary>
            public event EventHandler<object> ChangeVisualParams;

            /// <summary>
            /// Raised when the Skia surface is ready to be painted.
            /// The <see cref="IDisposable"/> argument is the Skia canvas surface.
            /// </summary>
            public event EventHandler<IDisposable> PaintSurface;

            /// <summary>Current horizontal DPI.</summary>
            public double GetDpiX() => _owner.GetDpiX();

            /// <summary>Current vertical DPI.</summary>
            public double GetDpiY() => _owner.GetDpiY();

            
            /// <summary>
            /// Requests a redraw of the underlying Skia element.
            /// Safe to call from any thread (marshals to UI thread if needed).
            /// </summary>
            public void Invalidate()
            {
                var canvas = _owner._skiaCanvas;
                if (canvas == null) return;

                // If we're already on the UI thread, invalidate immediately.
                if (canvas.Dispatcher.CheckAccess())
                {
                    canvas.InvalidateVisual();
                    return;
                }

                // Otherwise marshal to the UI thread at Render priority.
                canvas.Dispatcher.BeginInvoke(
                    (Action)(() => canvas.InvalidateVisual()),
                    DispatcherPriority.Render
                );
            }

            /// <summary>
            /// Sets the mouse cursor according to the requested domain cursor type.
            /// </summary>
            public void SetCursor(Abstractions.Ui.Enums.CursorType cursorType) => _owner.SetCursor(cursorType);

            /// <summary>
            /// Raises <see cref="ChangeVisualParams"/>.
            /// </summary>
            public void OnVisualParamsChanged()
            {
                var handler = ChangeVisualParams;
                handler?.Invoke(this, EventArgs.Empty);
            }

            /// <summary>
            /// Forwards Skia paint requests to subscribers.
            /// </summary>
            public void DoPaintSurface(object sender, SKPaintSurfaceEventArgs e)
            {
                var handler = PaintSurface;
                handler?.Invoke(this, e.Surface.Canvas);
            }
        }

        /// <summary>
        /// Internal entry point for redraw operations.
        /// Use <c>Redrawable.Invalidate()</c> instead of calling the control directly.
        /// </summary>
        internal RedrawableAdapter Redrawable { get; }        

        /// <summary>
        /// Optional generic exposure of the adapter (API symmetry with UWP).
        /// </summary>
        public object CompositionAdapter => Redrawable;

        #endregion Composition

        #region Properties

        /// <summary>Current width of the Skia canvas in device-independent pixels.</summary>
        public double CurrentWidth => _skiaCanvas != null ? _skiaCanvas.ActualWidth : 0;

        /// <summary>Current height of the Skia canvas in device-independent pixels.</summary>
        public double CurrentHeight => _skiaCanvas != null ? _skiaCanvas.ActualHeight : 0;

        /// <summary>Current horizontal DPI.</summary>
        public double GetDpiX() => _dpiX;

        /// <summary>Current vertical DPI.</summary>
        public double GetDpiY() => _dpiY;

        #endregion Properties

        #region Events

        /// <summary>
        /// Raised when visual parameters change (size/DPI).
        /// </summary>
        public event EventHandler<object> ChangeVisualParams = delegate { };

        /// <summary>
        /// Raised when the Skia surface is ready to be painted.
        /// The <see cref="IDisposable"/> argument is the Skia canvas surface.
        /// </summary>
        public event EventHandler<IDisposable> PaintSurface = delegate { };

        #endregion Events

        #region Initialization

        /// <summary>
        /// Static ctor: hooks the default style key for this control type.
        /// </summary>
        static PhialeDrawBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PhialeDrawBox),
                new FrameworkPropertyMetadata(typeof(PhialeDrawBox)));
        }

        /// <summary>
        /// Instance ctor: sets up composition adapter, DPI and input hooks.
        /// </summary>
        public PhialeDrawBox()
        {
            Redrawable = new RedrawableAdapter(this);
            InitializeDpi();
            this.IsManipulationEnabled = true;

            this.Loaded += (s, e) =>
            {
                // Track DPI changes on the hosting window.
                var w = Window.GetWindow(this);
                if (w != null) w.DpiChanged += DoDpiChanged;

                // Mouse hooks used by higher-level interaction logic (e.g., drags).
                MouseLeftButtonDown += OnMouseLeftButtonDown;
                MouseRightButtonDown += OnMouseRightButtonDown;
            };

            this.Unloaded += (s, e) =>
            {
                var w = Window.GetWindow(this);
                if (w != null) w.DpiChanged -= DoDpiChanged;

                MouseLeftButtonDown -= OnMouseLeftButtonDown;
                MouseRightButtonDown -= OnMouseRightButtonDown;
            };
        }

        #endregion Initialization

        #region Dependency Properties

        /// <summary>
        /// Interaction manager used to coordinate user input and rendering logic.
        /// Registered/unregistered against the internal <see cref="IRenderingComposition"/> adapter.
        /// </summary>
        public IGisInteractionManager? GisInteractionManager
        {
            get => (IGisInteractionManager?)GetValue(GisInteractionManagerProperty);
            set => SetValue(GisInteractionManagerProperty, value);
        }

        /// <summary>
        /// Backing store for <see cref="GisInteractionManager"/>.
        /// </summary>
        public static readonly DependencyProperty GisInteractionManagerProperty =
            DependencyProperty.Register(
                "GisInteractionManager",
                typeof(IGisInteractionManager),
                typeof(PhialeDrawBox),
                new PropertyMetadata(null, OnGisInteractionManagerChanged));

        /// <summary>
        /// When true, mouse events generated by stylus/touch are forwarded/ignored according to higher-level logic.
        /// </summary>
        public static readonly DependencyProperty ForwardMouseEventsOnPenOrTouchInteractionPropety =
            DependencyProperty.Register(
                "IgnoreMouseEventsFromStylus",
                typeof(bool),
                typeof(PhialeDrawBox),
                new PropertyMetadata(false));

        /// <summary>
        /// CLR wrapper for <see cref="ForwardMouseEventsOnPenOrTouchInteractionPropety"/>.
        /// </summary>
        public bool ForwardMouseEventsOnPenOrTouchInteraction
        {
            get => (bool)GetValue(ForwardMouseEventsOnPenOrTouchInteractionPropety);
            set => SetValue(ForwardMouseEventsOnPenOrTouchInteractionPropety, value);
        }

        /// <summary>
        /// DP callback — unregisters the old manager and registers the new one
        /// on the internal <see cref="IRenderingComposition"/> adapter (not on the control).
        /// </summary>
        private static void OnGisInteractionManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewport = d as PhialeDrawBox;
            var adapter = viewport?.Redrawable; // IRenderingComposition

            var oldManager = e.OldValue as IGisInteractionManager;
            var newManager = e.NewValue as IGisInteractionManager;

            if (viewport != null)
                viewport.AttachInteractionManager(oldManager, newManager);

            if (oldManager != null && adapter != null)
                oldManager.UnregisterControl(adapter);

            if (newManager != null && adapter != null)
                newManager.RegisterControl(adapter);

            viewport?.RefreshInteractionStatus();
            viewport?.Invalidate();
        }

        /// <summary>
        /// Controls visibility of the vertical ScrollBar template part.
        /// </summary>
        public Visibility VerticalScrollBarVisible
        {
            get => (Visibility)GetValue(VerticalScrollBarVisibleProperty);
            set => SetValue(VerticalScrollBarVisibleProperty, value);
        }

        /// <summary>Backing store for <see cref="VerticalScrollBarVisible"/>.</summary>
        public static readonly DependencyProperty VerticalScrollBarVisibleProperty =
            DependencyProperty.Register(
                "VerticalScrollBarVisible",
                typeof(Visibility),
                typeof(PhialeDrawBox),
                new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Controls visibility of the horizontal ScrollBar template part.
        /// </summary>
        public Visibility HorizontalScrollBarVisible
        {
            get => (Visibility)GetValue(HorizontalScrollBarVisibleProperty);
            set => SetValue(HorizontalScrollBarVisibleProperty, value);
        }

        /// <summary>Backing store for <see cref="HorizontalScrollBarVisible"/>.</summary>
        public static readonly DependencyProperty HorizontalScrollBarVisibleProperty =
            DependencyProperty.Register(
                "HorizontalScrollBarVisible",
                typeof(Visibility),
                typeof(PhialeDrawBox),
                new PropertyMetadata(Visibility.Collapsed));

        #endregion Dependency Properties

        #region Manipulation Handlers

        /// <summary>
        /// Adds a lightweight bridge to WPF <see cref="UIElement.ManipulationDelta"/> as domain event args.
        /// </summary>
        public void AddManipulationDeltaHandler(Action<PhManipulationDeltaEventArgs> handler)
        {
            this.ManipulationDelta += (s, e) =>
            {
                var points = e.Manipulators
                              .Select(m => m.GetPosition(this))
                              .Select(p => new Core.Models.Geometry.Point(p.X, p.Y))
                              .ToArray();

                handler?.Invoke(new PhManipulationDeltaEventArgs(points));
            };
        }

        /// <summary>
        /// Adds a lightweight bridge to WPF <see cref="UIElement.ManipulationCompleted"/> as domain event args.
        /// </summary>
        public void AddManipulationCompletedHandler(Action<PhManipulationDeltaEventArgs> handler)
        {
            this.ManipulationCompleted += (s, e) =>
            {
                var points = e.Manipulators
                              .Select(m => m.GetPosition(this))
                              .Select(p => new Core.Models.Geometry.Point(p.X, p.Y))
                              .ToArray();

                handler?.Invoke(new PhManipulationDeltaEventArgs(points));
            };
        }

        #endregion Manipulation Handlers

        #region Utility Methods

        /// <summary>
        /// Initializes DPI from the current <see cref="PresentationSource"/>.
        /// Falls back to 96 DPI when composition target is unavailable.
        /// </summary>
        private void InitializeDpi()
        {
            var ps = PresentationSource.FromVisual(this);
            if (ps?.CompositionTarget != null)
            {
                _dpiX = 96.0 * ps.CompositionTarget.TransformToDevice.M11;
                _dpiY = 96.0 * ps.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                _dpiX = _dpiY = 96.0;
            }
        }

        /// <summary>
        /// Convenience wrapper for legacy call sites —
        /// prefer <c>Redrawable.Invalidate()</c>.
        /// </summary>
        internal void Invalidate() => Redrawable.Invalidate();

        /// <summary>
        /// Sets the mouse cursor according to the requested domain cursor type.
        /// </summary>
        private void SetCursor(PhialeGis.Library.Abstractions.Ui.Enums.CursorType cursorType)
        {
            void set(Cursor c) => Application.Current.Dispatcher.Invoke(() => Mouse.OverrideCursor = c);
            if (cursorType == PhialeGis.Library.Abstractions.Ui.Enums.CursorType.Hand) set(Cursors.Hand);
            else set(Cursors.Arrow);
        }

        #endregion Utility Methods

        #region Event Handlers

        /// <summary>
        /// Host window DPI changed — update cached DPI and notify listeners.
        /// </summary>
        private void DoDpiChanged(object sender, DpiChangedEventArgs e)
        {
            _dpiX = e.NewDpi.PixelsPerInchX;
            _dpiY = e.NewDpi.PixelsPerInchY;
            Redrawable.OnVisualParamsChanged();
        }

        /// <summary>
        /// Forwards Skia paint requests to subscribers.
        /// </summary>
        private void DoPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var handler = this.PaintSurface;
            handler?.Invoke(this, e.Surface.Canvas);
        }

        /// <summary>
        /// Handles horizontal ScrollBar movement and emits logical translation.
        /// </summary>
        private void DoHorizontalScrollBarScroll(object sender, ScrollEventArgs e)
        {
            double offsetX = e.NewValue - _lastScrolledValueX;
            var translationX = offsetX * this.ActualWidth;
            _lastScrolledValueX = e.NewValue;
            Redrawable.RaiseSurfaceShifted(translationX, 0);
        }

        /// <summary>
        /// Handles vertical ScrollBar movement and emits logical translation.
        /// </summary>
        private void DoVerticalScrollBarScroll(object sender, ScrollEventArgs e)
        {
            double offsetY = _lastScrolledValueY - e.NewValue;
            var translationY = offsetY * this.ActualHeight;
            _lastScrolledValueY = e.NewValue;
            Redrawable.RaiseSurfaceShifted(0, translationY);
        }

        #endregion Event Handlers

        #region Overrides

        /// <summary>
        /// Resolves template parts and wires up event handlers.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollBarVertical = GetTemplateChild("ScrollBarVertical") as ScrollBar;
            _scrollBarHorizontal = GetTemplateChild("ScrollBarHorizontal") as ScrollBar;

            if (_scrollBarVertical != null)
                _scrollBarVertical.Scroll += DoVerticalScrollBarScroll;

            if (_scrollBarHorizontal != null)
                _scrollBarHorizontal.Scroll += DoHorizontalScrollBarScroll;

            _skiaCanvas = GetTemplateChild("SkiaCanvas") as SKElement;
            if (_skiaCanvas != null)
                _skiaCanvas.PaintSurface += Redrawable.DoPaintSurface;

            _statusPrimaryText = GetTemplateChild("PART_StatusPrimaryText") as TextBlock;
            _statusCoordinateText = GetTemplateChild("PART_StatusCoordinateText") as TextBlock;
            _statusSnapText = GetTemplateChild("PART_StatusSnapText") as TextBlock;

            if (_takeoverButton != null) _takeoverButton.Click -= OnTakeoverClicked;
            if (_undoButton != null) _undoButton.Click -= OnUndoClicked;
            if (_finishButton != null) _finishButton.Click -= OnFinishClicked;
            if (_cancelButton != null) _cancelButton.Click -= OnCancelClicked;

            _takeoverButton = GetTemplateChild("PART_TakeoverButton") as Button;
            _undoButton = GetTemplateChild("PART_UndoButton") as Button;
            _finishButton = GetTemplateChild("PART_FinishButton") as Button;
            _cancelButton = GetTemplateChild("PART_CancelButton") as Button;

            if (_takeoverButton != null) _takeoverButton.Click += OnTakeoverClicked;
            if (_undoButton != null) _undoButton.Click += OnUndoClicked;
            if (_finishButton != null) _finishButton.Click += OnFinishClicked;
            if (_cancelButton != null) _cancelButton.Click += OnCancelClicked;

            RefreshInteractionStatus();
        }

        /// <summary>
        /// Notifies listeners when layout size changes.
        /// </summary>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            Redrawable.OnVisualParamsChanged();
        }

        private void AttachInteractionManager(IGisInteractionManager? oldManager, IGisInteractionManager? newManager)
        {
            if (oldManager != null)
                oldManager.ViewportInteractionStatusChanged -= OnViewportInteractionStatusChanged;

            if (newManager != null)
                newManager.ViewportInteractionStatusChanged += OnViewportInteractionStatusChanged;
        }

        private void OnViewportInteractionStatusChanged(object? sender, ViewportInteractionStatusChangedEventArgs e)
        {
            if (e?.TargetDraw != null && !ReferenceEquals(e.TargetDraw, Redrawable))
                return;

            Dispatcher.BeginInvoke((Action)RefreshInteractionStatus);
        }

        private void RefreshInteractionStatus()
        {
            var manager = GisInteractionManager;
            if (manager == null || !manager.TryGetViewportInteractionStatus(Redrawable, out var status))
            {
                ApplyInteractionStatus(null);
                return;
            }

            ApplyInteractionStatus(status);
        }

        private void ApplyInteractionStatus(ViewportInteractionStatus? status)
        {
            if (_statusPrimaryText != null)
                _statusPrimaryText.Text = status == null
                    ? string.Empty
                    : BuildPrimaryStatusText(status);

            if (_statusCoordinateText != null)
                _statusCoordinateText.Text = status?.CoordinateText ?? string.Empty;

            if (_statusSnapText != null)
                _statusSnapText.Text = status?.SnapText ?? string.Empty;

            ApplyButtonState(_takeoverButton, status?.CanTakeOver == true, true, "Przejmij rysowanie");
            ApplyCommandButtonState(_undoButton, status, "undo", "Cofnij");
            ApplyCommandButtonState(_finishButton, status, "enter", "Zatwierdz");
            ApplyCommandButtonState(_cancelButton, status, "cancel", "Anuluj");
        }

        private static string BuildPrimaryStatusText(ViewportInteractionStatus status)
        {
            if (status == null || !status.HasActiveSession)
                return "Brak aktywnej akcji";

            if (string.IsNullOrWhiteSpace(status.PromptText))
                return status.ActionName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(status.ActionName))
                return status.PromptText;

            return status.ActionName + " | " + status.PromptText;
        }

        private void ApplyCommandButtonState(Button? button, ViewportInteractionStatus? status, string commandId, string fallbackLabel)
        {
            if (button == null)
                return;

            var item = FindCommand(status, commandId);
            if (item == null || status == null || !status.IsInputViewport)
            {
                button.Visibility = Visibility.Collapsed;
                button.IsEnabled = false;
                button.Content = fallbackLabel;
                return;
            }

            button.Visibility = Visibility.Visible;
            button.IsEnabled = item.Enabled;
            button.Content = string.IsNullOrWhiteSpace(item.Label) ? fallbackLabel : item.Label;
        }

        private static void ApplyButtonState(Button? button, bool visible, bool enabled, string label)
        {
            if (button == null)
                return;

            button.Content = label;
            button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            button.IsEnabled = enabled;
        }

        private static ActionContextMenuItem? FindCommand(ViewportInteractionStatus? status, string commandId)
        {
            var items = status?.Commands;
            if (items == null)
                return null;

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item != null && string.Equals(item.CommandId, commandId, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        private void OnTakeoverClicked(object sender, RoutedEventArgs e)
        {
            GisInteractionManager?.TryTakeoverInteractiveSession(Redrawable);
        }

        private void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            GisInteractionManager?.TryHandleInteractiveMenuCommand(Redrawable, "undo");
        }

        private void OnFinishClicked(object sender, RoutedEventArgs e)
        {
            GisInteractionManager?.TryHandleInteractiveMenuCommand(Redrawable, "enter");
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            GisInteractionManager?.TryHandleInteractiveMenuCommand(Redrawable, "cancel");
        }

        #endregion Overrides
    }
}
