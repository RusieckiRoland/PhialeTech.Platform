// PhialeGis.Library.WinUi/Controls/PhialeDrawBoxWinUI.cs
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using SkiaSharp.Views.Windows; // SKXamlCanvas (WinUI 3)
using System;

namespace PhialeGis.Library.WinUi.Controls
{
    /// <summary>
    /// WinUI 3 drawing host control backed by SkiaSharp.
    /// Composition pattern exposes IRenderingComposition without leaking non-WinRT types.
    /// </summary>
    [TemplatePart(Name = "ScrollBarVertical", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "ScrollBarHorizontal", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "SkiaCanvas", Type = typeof(SKXamlCanvas))]
    public sealed partial class PhialeDrawBoxWinUI : Control
    {
        private ScrollBar? _scrollBarVertical;
        private ScrollBar? _scrollBarHorizontal;
        internal SKXamlCanvas? _skiaCanvas;
        private TextBlock? _statusPrimaryText;
        private TextBlock? _statusCoordinateText;
        private TextBlock? _statusSnapText;
        private Button? _takeoverButton;
        private Button? _undoButton;
        private Button? _finishButton;
        private Button? _cancelButton;

        private double _dpiX = 96.0;
        private double _dpiY = 96.0;

        private double _lastScrolledValueX = 0;
        private double _lastScrolledValueY = 0;

        // -------- Composition (rendering only; input w partialu Events) --------
        internal sealed partial class RedrawableAdapter : IRenderingComposition
        {
            private readonly PhialeDrawBoxWinUI _owner;
            public RedrawableAdapter(PhialeDrawBoxWinUI owner) => _owner = owner;

            public double CurrentWidth => _owner.CurrentWidth;
            public double CurrentHeight => _owner.CurrentHeight;

            public event EventHandler<object>? ChangeVisualParams;
            public event EventHandler<IDisposable>? PaintSurface;

            public double GetDpiX() => _owner.GetDpiX();
            public double GetDpiY() => _owner.GetDpiY();

            /// <summary>Request redraw of the Skia surface.</summary>
            public void Invalidate() => _owner._skiaCanvas?.Invalidate();

            /// <summary>Set pointer cursor.</summary>
            public void SetCursor(CursorType cursorType) => _owner.SetCursor(cursorType);

            internal void OnVisualParamsChanged() => ChangeVisualParams?.Invoke(this, EventArgs.Empty);

            internal void DoPaintSurface(object? sender, SkiaSharp.Views.Windows.SKPaintSurfaceEventArgs e)
                => PaintSurface?.Invoke(this, e.Surface.Canvas);
        }

        internal RedrawableAdapter Redrawable { get; }
        public object CompositionAdapter => Redrawable;

        // -------- Public surface (DPI/size) -----------------------------------
        public double CurrentWidth => _skiaCanvas?.ActualWidth ?? 0;
        public double CurrentHeight => _skiaCanvas?.ActualHeight ?? 0;

        public double GetDpiX() => _dpiX;
        public double GetDpiY() => _dpiY;

        public PhialeDrawBoxWinUI()
        {
            DefaultStyleKey = typeof(PhialeDrawBoxWinUI);
            Redrawable = new RedrawableAdapter(this);

            // default gestures like UWP sample
            ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY |
                               ManipulationModes.Scale | ManipulationModes.Rotate;

            SizeChanged += (_, __) => { UpdateDpi(); Redrawable.OnVisualParamsChanged(); };
            Loaded += (_, __) => UpdateDpi();
        }

        private void UpdateDpi()
        {
            var scale = XamlRoot?.RasterizationScale ?? 1.0;
            _dpiX = 96.0 * scale;
            _dpiY = 96.0 * scale;
        }

        internal void Invalidate() => Redrawable.Invalidate();

        internal void SetCursor(CursorType cursorType)
        {
            try
            {
                var shape = cursorType == CursorType.Hand ? InputSystemCursorShape.Hand : InputSystemCursorShape.Arrow;
                ProtectedCursor = InputSystemCursor.Create(shape);
            }
            catch { /* ignore */ }
        }

        // -------- Template wiring ---------------------------------------------
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_scrollBarVertical != null) _scrollBarVertical.Scroll -= DoVerticalScrollBarScroll;
            if (_scrollBarHorizontal != null) _scrollBarHorizontal.Scroll -= DoHorizontalScrollBarScroll;
            if (_skiaCanvas != null) _skiaCanvas.PaintSurface -= Redrawable.DoPaintSurface;

            _scrollBarVertical = GetTemplateChild("ScrollBarVertical") as ScrollBar;
            _scrollBarHorizontal = GetTemplateChild("ScrollBarHorizontal") as ScrollBar;
            _skiaCanvas = GetTemplateChild("SkiaCanvas") as SKXamlCanvas;
            _statusPrimaryText = GetTemplateChild("PART_StatusPrimaryText") as TextBlock;
            _statusCoordinateText = GetTemplateChild("PART_StatusCoordinateText") as TextBlock;
            _statusSnapText = GetTemplateChild("PART_StatusSnapText") as TextBlock;

            if (_scrollBarVertical != null) _scrollBarVertical.Scroll += DoVerticalScrollBarScroll;
            if (_scrollBarHorizontal != null) _scrollBarHorizontal.Scroll += DoHorizontalScrollBarScroll;
            if (_skiaCanvas != null) _skiaCanvas.PaintSurface += Redrawable.DoPaintSurface;

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

        // -------- Scrollbars → logical surface shift --------------------------
        private void DoHorizontalScrollBarScroll(object sender, ScrollEventArgs e)
        {
            double offsetX = e.NewValue - _lastScrolledValueX;
            _lastScrolledValueX = e.NewValue;
            var translationX = offsetX * ActualWidth;
            Redrawable_RaiseSurfaceShifted(translationX, 0);
        }

        private void DoVerticalScrollBarScroll(object sender, ScrollEventArgs e)
        {
            double offsetY = _lastScrolledValueY - e.NewValue;
            _lastScrolledValueY = e.NewValue;
            var translationY = offsetY * ActualHeight;
            Redrawable_RaiseSurfaceShifted(0, translationY);
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

            _ = DispatcherQueue.TryEnqueue(RefreshInteractionStatus);
        }

        private void RefreshInteractionStatus()
        {
            var manager = InteractionManagerInternal;
            if (manager == null || !manager.TryGetViewportInteractionStatus(Redrawable, out var status))
            {
                ApplyInteractionStatus(null);
                return;
            }

            ApplyInteractionStatus(status);
        }

        private void ApplyInteractionStatus(PhialeGis.Library.Abstractions.Interactions.ViewportInteractionStatus? status)
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

        private static string BuildPrimaryStatusText(PhialeGis.Library.Abstractions.Interactions.ViewportInteractionStatus status)
        {
            if (status == null || !status.HasActiveSession)
                return "Brak aktywnej akcji";

            if (string.IsNullOrWhiteSpace(status.PromptText))
                return status.ActionName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(status.ActionName))
                return status.PromptText;

            return status.ActionName + " | " + status.PromptText;
        }

        private void ApplyCommandButtonState(Button? button, PhialeGis.Library.Abstractions.Interactions.ViewportInteractionStatus? status, string commandId, string fallbackLabel)
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

        private static Abstractions.Actions.ActionContextMenuItem? FindCommand(PhialeGis.Library.Abstractions.Interactions.ViewportInteractionStatus? status, string commandId)
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
            InteractionManagerInternal?.TryTakeoverInteractiveSession(Redrawable);
        }

        private void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            InteractionManagerInternal?.TryHandleInteractiveMenuCommand(Redrawable, "undo");
        }

        private void OnFinishClicked(object sender, RoutedEventArgs e)
        {
            InteractionManagerInternal?.TryHandleInteractiveMenuCommand(Redrawable, "enter");
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            InteractionManagerInternal?.TryHandleInteractiveMenuCommand(Redrawable, "cancel");
        }
    }
}
