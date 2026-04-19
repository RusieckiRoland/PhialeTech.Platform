// PhialeDrawBoxAvalonia.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;
using PhialeGis.Library.AvaloniaUi.Converters;
using PhialeGis.Library.Core.Interactions;
using SkiaSharp;
using System;

namespace PhialeGis.Library.AvaloniaUi.Controls
{
    /// <summary>
    /// Avalonia drawing host control backed by Skia (via Avalonia.Skia).
    /// Uses composition for <see cref="IRenderingComposition"/> via an internal adapter.
    /// </summary>
    public sealed partial class PhialeDrawBoxAvalonia : TemplatedControl
    {
        #region Private Fields

        private double _dpiX;
        private double _dpiY;
        private double _lastScrolledValueX = 0;
        private double _lastScrolledValueY = 0;
        private ScrollBar? _scrollBarHorizontal;
        private ScrollBar? _scrollBarVertical;
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
        /// Optional generic exposure of the adapter (API symmetry).
        /// </summary>
        public object CompositionAdapter => Redrawable;

        /// <summary>
        /// Internal composition entry point for redraw operations.
        /// Use <c>Redrawable.Invalidate()</c> instead of calling the control directly.
        /// </summary>
        internal RedrawableAdapter Redrawable { get; }

        internal sealed partial class RedrawableAdapter : IRenderingComposition
        {
            private readonly PhialeDrawBoxAvalonia _owner;
            

            public RedrawableAdapter(PhialeDrawBoxAvalonia owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
               
            }

            public event EventHandler<object>? ChangeVisualParams;

            public event EventHandler<IDisposable>? PaintSurface;

            public double CurrentHeight => _owner.CurrentHeight;
            public double CurrentWidth => _owner.CurrentWidth;

            public double GetDpiX() => _owner.GetDpiX();

            public double GetDpiY() => _owner.GetDpiY();

            /// <summary>
            /// Requests a redraw of the underlying Skia surface.
            /// Safe to call from the UI thread.
            /// </summary>
            public void Invalidate()
            {
                if (Dispatcher.UIThread.CheckAccess())
                    _owner.InvalidateVisual();
                else
                    Dispatcher.UIThread.Post(_owner.InvalidateVisual, DispatcherPriority.Render);
            }

            /// <summary>
            /// Raises <see cref="ChangeVisualParams"/>.
            /// </summary>
            public void OnVisualParamsChanged()
            {
                ChangeVisualParams?.Invoke(this, EventArgs.Empty);
            }

            public void SetCursor(CursorType cursorType)
            {
                _owner.SetCursor(cursorType);
            }

            /// <summary>
            /// Forwards Skia paint requests to subscribers.
            /// </summary>
            internal void RaisePaintSurface(IDisposable canvas)
            {
                PaintSurface?.Invoke(this, canvas);
            }
        }

        #endregion Composition

        #region Properties

        /// <summary>
        /// Current height of the drawing surface in device-independent pixels.
        /// </summary>
        public double CurrentHeight => Bounds.Height;

        /// <summary>
        /// Current width of the drawing surface in device-independent pixels.
        /// </summary>
        public double CurrentWidth => Bounds.Width;

        /// <summary>
        /// Current horizontal DPI of the display.
        /// </summary>
        public double GetDpiX() => _dpiX;

        /// <summary>
        /// Current vertical DPI of the display.
        /// </summary>
        public double GetDpiY() => _dpiY;

        #endregion Properties

        #region Events

        /// <summary>
        /// Raised when visual parameters change (size/DPI).
        /// </summary>
        public event EventHandler<object>? ChangeVisualParams;

        /// <summary>
        /// Raised when the Skia surface is ready to be painted.
        /// The <see cref="IDisposable"/> argument is the Skia canvas.
        /// </summary>
        public event EventHandler<IDisposable>? PaintSurface;

        #endregion Events

        #region Initialization

        public PhialeDrawBoxAvalonia()
        {
            Redrawable = new RedrawableAdapter(this);

            InitializeDpi();

            // Mirror UWP: react to size changes
            this.GetObservable(BoundsProperty).Subscribe(_ =>
            {
                InitializeDpi();
                Redrawable.OnVisualParamsChanged();
                ChangeVisualParams?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
            });

            // Avalonia: react to RenderScaling changes (per-monitor DPI)
            this.AttachedToVisualTree += (_, __) =>
            {
                InitializeDpi();
                var tl = TopLevel.GetTopLevel(this);
                if (tl is not null)
                {
                    tl.PropertyChanged += (_, e) =>
                    {
                        if (e.Property?.Name == "RenderScaling")
                        {
                            InitializeDpi();
                            Redrawable.OnVisualParamsChanged();
                            ChangeVisualParams?.Invoke(this, EventArgs.Empty);
                            InvalidateVisual();
                        }
                    };
                }
            };
        }

        #endregion Initialization

        #region Utility Methods

        internal void Invalidate() => Redrawable.Invalidate();

        internal void SetCursor(CursorType cursorType)
        {
            Cursor = cursorType == CursorType.Hand
                ? new Cursor(StandardCursorType.Hand)
                : new Cursor(StandardCursorType.Arrow);
        }

        private void InitializeDpi()
        {
            var tl = TopLevel.GetTopLevel(this);
            var scale = tl?.RenderScaling ?? 1.0;
            _dpiX = _dpiY = 96.0 * scale;
        }

        #endregion Utility Methods

        #region Event Handlers

        private void DoHorizontalScrollBarScroll(double newValue)
        {
            double offsetX = newValue - _lastScrolledValueX;
            var translationX = offsetX * this.Bounds.Width;
            _lastScrolledValueX = newValue;
            Redrawable.RaiseSurfaceShifted(translationX, 0);
        }

        private void DoVerticalScrollBarScroll(double newValue)
        {
            double offsetY = _lastScrolledValueY - newValue;
            var translationY = offsetY * this.Bounds.Height;
            _lastScrolledValueY = newValue;
            Redrawable.RaiseSurfaceShifted(0, translationY);
        }

        #endregion Event Handlers

        #region Overrides (template + render)

        public override void Render(DrawingContext context)
        {
            // clear background (keeps parity with UWP SKXamlCanvas full repaint)
            context.FillRectangle(Background ?? Brushes.Transparent, new Rect(Bounds.Size));

            base.Render(context);

            // Skia drawing lease (equivalent of SKXamlCanvas.PaintSurface)
            context.Custom(new SkiaPaintOp(this, Bounds));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _scrollBarVertical = e.NameScope.Find<ScrollBar>("ScrollBarVertical");
            _scrollBarHorizontal = e.NameScope.Find<ScrollBar>("ScrollBarHorizontal");
            _statusPrimaryText = e.NameScope.Find<TextBlock>("PART_StatusPrimaryText");
            _statusCoordinateText = e.NameScope.Find<TextBlock>("PART_StatusCoordinateText");
            _statusSnapText = e.NameScope.Find<TextBlock>("PART_StatusSnapText");

            if (_takeoverButton != null) _takeoverButton.Click -= OnTakeoverClicked;
            if (_undoButton != null) _undoButton.Click -= OnUndoClicked;
            if (_finishButton != null) _finishButton.Click -= OnFinishClicked;
            if (_cancelButton != null) _cancelButton.Click -= OnCancelClicked;

            _takeoverButton = e.NameScope.Find<Button>("PART_TakeoverButton");
            _undoButton = e.NameScope.Find<Button>("PART_UndoButton");
            _finishButton = e.NameScope.Find<Button>("PART_FinishButton");
            _cancelButton = e.NameScope.Find<Button>("PART_CancelButton");

            if (_takeoverButton != null) _takeoverButton.Click += OnTakeoverClicked;
            if (_undoButton != null) _undoButton.Click += OnUndoClicked;
            if (_finishButton != null) _finishButton.Click += OnFinishClicked;
            if (_cancelButton != null) _cancelButton.Click += OnCancelClicked;

            if (_scrollBarVertical is not null)
            {
                _scrollBarVertical.PropertyChanged += (s, args) =>
                {
                    if (args.Property == RangeBase.ValueProperty && args.NewValue is double v)
                        DoVerticalScrollBarScroll(v);
                };
            }

            if (_scrollBarHorizontal is not null)
            {
                _scrollBarHorizontal.PropertyChanged += (s, args) =>
                {
                    if (args.Property == RangeBase.ValueProperty && args.NewValue is double v)
                        DoHorizontalScrollBarScroll(v);
                };
            }

            RefreshInteractionStatus();
        }

        private sealed class SkiaPaintOp : ICustomDrawOperation
        {
            private readonly PhialeDrawBoxAvalonia _owner;

            public SkiaPaintOp(PhialeDrawBoxAvalonia owner, Rect bounds)
            {
                _owner = owner;
                Bounds = bounds;
            }

            public Rect Bounds { get; }

            public void Dispose()
            { }

            public bool Equals(ICustomDrawOperation? other) => false;

            public bool HitTest(Point p) => Bounds.Contains(p);

            public void Render(ImmediateDrawingContext context)
            {
                if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature)) return;
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                // Clip to control bounds
                canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height),
                                SKClipOperation.Intersect, antialias: true);

                // Forward to composition adapter
                _owner.PaintSurface?.Invoke(_owner, canvas);
                _owner.Redrawable?.RaisePaintSurface(canvas);
            }
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

            Dispatcher.UIThread.Post(RefreshInteractionStatus, DispatcherPriority.Background);
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
                _statusPrimaryText.Text = status == null ? string.Empty : BuildPrimaryStatusText(status);

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
                button.IsVisible = false;
                button.IsEnabled = false;
                button.Content = fallbackLabel;
                return;
            }

            button.IsVisible = true;
            button.IsEnabled = item.Enabled;
            button.Content = string.IsNullOrWhiteSpace(item.Label) ? fallbackLabel : item.Label;
        }

        private static void ApplyButtonState(Button? button, bool visible, bool enabled, string label)
        {
            if (button == null)
                return;

            button.Content = label;
            button.IsVisible = visible;
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

        private void OnTakeoverClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GisInteractionManager?.TryTakeoverInteractiveSession(Redrawable);
        }

        private void OnUndoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GisInteractionManager?.TryHandleInteractiveMenuCommand(Redrawable, "undo");
        }

        private void OnFinishClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GisInteractionManager?.TryHandleInteractiveMenuCommand(Redrawable, "enter");
        }

        private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GisInteractionManager?.TryHandleInteractiveMenuCommand(Redrawable, "cancel");
        }

        
        #endregion Overrides (template + render)
    }
}

