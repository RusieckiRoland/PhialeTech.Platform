using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Interop;
using System.Windows.Threading;
using PhialeGrid.Core;
using PhialeGrid.Core.HitTesting;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Diagnostics;
using UniversalInput.Contracts;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    /// <summary>
    /// Thin WPF host for the grid surface. Platform events are mapped to shared input contracts
    /// and interpreted in Core.
    /// </summary>
    public sealed partial class GridSurfaceHost : UserControl
    {
        private readonly GridSurfaceUniversalInputAdapter _coreInputAdapter = new GridSurfaceUniversalInputAdapter();
        private readonly GridHitTestingService _hitTesting = new GridHitTestingService();
        private const int WmLButtonDown = 0x0201;
        private GridSurfaceCoordinator _coordinator;
        private bool _isApplyingFocusRequest;
        private bool _isApplyingScrollState;
        private bool _isChangingPointerCapture;
        private HwndSource _hwndSource;
        private PlatformPointerCaptureSession _activePointerCapture;
        private string _lastFocusedEditingCellKey;
        private bool _wasInEditMode;
        private bool _isHorizontalScrollbarInteractionActive;

        public GridSurfaceHost()
        {
            InitializeComponent();
            AutomationProperties.SetAutomationId(this, "phiale-grid.surface");
            AutomationProperties.SetName(this, "Grid surface");
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            Focusable = true;
            IsManipulationEnabled = true;
            SurfacePanel.CellEditingTextChanged += OnSurfaceCellEditingTextChanged;
            AddHandler(FrameworkElement.RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(OnRequestBringIntoView), true);
            AttachPlatformEventHandlers();
        }

        public void Initialize(GridSurfaceCoordinator coordinator)
        {
            if (coordinator == null)
            {
                throw new ArgumentNullException(nameof(coordinator));
            }

            DetachCoordinator();
            _coordinator = coordinator;
            _coordinator.SnapshotChanged += OnSnapshotChanged;
            _coordinator.FocusRequested += OnFocusRequested;

            SyncViewportSize();
            RenderCurrentSnapshot();
        }

        public GridSurfaceSnapshot CurrentSnapshot => _coordinator?.GetCurrentSnapshot();

        public GridSurfaceCoordinator Coordinator => _coordinator;

        public IGridRowDetailContentFactory RowDetailContentFactory
        {
            get { return SurfacePanel.RowDetailContentFactory; }
            set { SurfacePanel.RowDetailContentFactory = value; }
        }

        public double HorizontalOffset => ScrollViewer.HorizontalOffset;

        public double VerticalOffset => ScrollViewer.VerticalOffset;

        public double ViewportWidth => ScrollViewer.ViewportWidth > 0 ? ScrollViewer.ViewportWidth : ScrollViewer.ActualWidth;

        public double ViewportHeight => ScrollViewer.ViewportHeight > 0 ? ScrollViewer.ViewportHeight : ScrollViewer.ActualHeight;

        public double VerticalScrollBarGutterWidth => ScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible
            ? ResolveScrollBarThickness(Orientation.Vertical, SystemParameters.VerticalScrollBarWidth)
            : 0d;

        public double HorizontalScrollBarGutterHeight => ScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible
            ? ResolveScrollBarThickness(Orientation.Horizontal, SystemParameters.HorizontalScrollBarHeight)
            : 0d;

        public event ScrollChangedEventHandler ViewportScrollChanged;

        public event EventHandler HostGeometryChanged;

        internal GridSurfacePanel SurfacePanelForTesting => SurfacePanel;

        internal IGridSurfacePointerPositionResolver PointerPositionResolver { get; set; } = GridSurfacePointerPositionResolver.Default;

        internal IGridSurfacePointerCaptureController PointerCaptureController { get; set; } = GridSurfacePointerCaptureController.Default;

        internal void HandleWheelForTesting(UniversalPointerWheelChangedEventArgs args)
        {
            HandleWheel(args);
        }

        internal void HandlePointerPressedForTesting(UniversalPointerRoutedEventArgs args)
        {
            HandlePointerPressed(args);
        }

        internal bool HandleExternalPointerPressed(UniversalPointerRoutedEventArgs args)
        {
            return HandlePointerPressed(args, GridHitTestSurfaceScope.ColumnHeaderSurface);
        }

        internal void BeginExternalMousePointerCapture(Point position)
        {
            BeginMousePointerCapture(position);
        }

        internal void HandlePointerMovedForTesting(UniversalPointerRoutedEventArgs args)
        {
            HandlePointerMoved(args);
        }

        internal bool HandleExternalPointerMoved(UniversalPointerRoutedEventArgs args)
        {
            if (args?.Pointer?.Position != null)
            {
                UpdatePointerCursor(
                    new Point(args.Pointer.Position.X, args.Pointer.Position.Y),
                    GridHitTestSurfaceScope.ColumnHeaderSurface);
            }

            return HandlePointerMoved(args);
        }

        internal void UpdateExternalPointerPosition(Point position)
        {
            UpdateCapturedPointerPosition(position);
        }

        internal void HandlePointerReleasedForTesting(UniversalPointerRoutedEventArgs args)
        {
            HandlePointerReleased(args);
        }

        internal bool HandleExternalPointerReleased(UniversalPointerRoutedEventArgs args)
        {
            return HandlePointerReleased(args);
        }

        internal void EndExternalPointerCapture()
        {
            EndPointerCaptureSession();
        }

        internal void HandleKeyForTesting(UniversalKeyEventArgs args)
        {
            HandleKey(args);
        }

        internal void HandleTextForTesting(UniversalTextChangedEventArgs args)
        {
            HandleText(args);
        }

        private void AttachPlatformEventHandlers()
        {
            ScrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            ScrollViewer.SizeChanged += OnScrollViewerSizeChanged;
            PreviewMouseDown += OnPreviewMouseDown;
            PreviewMouseMove += OnPreviewMouseMove;
            PreviewMouseUp += OnPreviewMouseUp;
            PreviewMouseWheel += OnPreviewMouseWheel;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewTextInput += OnPreviewTextInput;
            PreviewTouchDown += OnPreviewTouchDown;
            PreviewTouchMove += OnPreviewTouchMove;
            PreviewTouchUp += OnPreviewTouchUp;
            ManipulationStarted += OnManipulationStarted;
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
            LostMouseCapture += OnLostMouseCapture;
            LostTouchCapture += OnLostTouchCapture;
            Unloaded += OnUnloaded;
        }

        private void DetachCoordinator()
        {
            if (_coordinator == null)
            {
                return;
            }

            _coordinator.SnapshotChanged -= OnSnapshotChanged;
            _coordinator.FocusRequested -= OnFocusRequested;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachWindowMessageHook();
            SyncViewportSize();
            RenderCurrentSnapshot();
            NotifyHostGeometryChanged();
            Focus();
        }

        private void OnSnapshotChanged(object sender, GridSnapshotChangedEventArgs e)
        {
            if (e?.Snapshot != null)
            {
                if (e.Snapshot.ViewportState.IsInEditMode)
                {
                    EndPointerCaptureSession();
                }

                SurfacePanel.RenderSnapshot(e.Snapshot);
                SyncScrollViewerToSnapshot(e.Snapshot);
                RenderViewportTrackMarkers(e.Snapshot);
                NotifyHostGeometryChanged();
                if (ShouldFocusEditingCellEditor(e.Snapshot))
                {
                    Dispatcher.BeginInvoke(new Action(() => SurfacePanel.FocusEditingCellEditor()), DispatcherPriority.Input);
                }

                _wasInEditMode = e.Snapshot.ViewportState.IsInEditMode;
                _lastFocusedEditingCellKey = GetEditingCellKey(e.Snapshot);
            }
        }

        private void OnSurfaceCellEditingTextChanged(object sender, Presenters.GridCellEditingTextChangedEventArgs e)
        {
            if (_coordinator == null || e == null)
            {
                return;
            }

            PhialeGridDiagnostics.Write(
                "GridSurfaceHost",
                $"Forwarding editor value input. Row='{e.RowKey}', Column='{e.ColumnKey}', Kind={e.ChangeKind}, Text='{e.Text ?? string.Empty}'.");

            var universalArgs = WpfUniversalInputAdapter.CreateEditorValueChangedEventArgs(
                e.RowKey,
                e.ColumnKey,
                e.Text,
                e.ChangeKind,
                Keyboard.Modifiers);

            _coordinator.ProcessInput(_coreInputAdapter.CreateEditorValueInput(
                universalArgs,
                DateTime.UtcNow));

            var appliedCell = _coordinator.GetCurrentSnapshot()?.Cells?.FirstOrDefault(cell =>
                string.Equals(cell.RowKey, e.RowKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(cell.ColumnKey, e.ColumnKey, StringComparison.OrdinalIgnoreCase));

            PhialeGridDiagnostics.Write(
                "GridSurfaceHost",
                $"Editor value input processed. Row='{e.RowKey}', Column='{e.ColumnKey}', Kind={e.ChangeKind}, SnapshotEditingText='{appliedCell?.EditingText ?? string.Empty}', DisplayText='{appliedCell?.DisplayText ?? string.Empty}', IsEditing={appliedCell?.IsEditing ?? false}.");
        }

        private void OnFocusRequested(object sender, GridFocusRequestEventArgs e)
        {
            if (_isApplyingFocusRequest)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isApplyingFocusRequest)
                {
                    return;
                }

                _isApplyingFocusRequest = true;
                try
                {
                    Focus();
                    Keyboard.Focus(this);
                }
                finally
                {
                    _isApplyingFocusRequest = false;
                }
            }), DispatcherPriority.Input);
        }

        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ViewportScrollChanged?.Invoke(this, e);
            NotifyHostGeometryChanged();

            if (_coordinator == null || _isApplyingScrollState)
            {
                return;
            }

            var snapshot = _coordinator.GetCurrentSnapshot();
            if (ShouldPreserveEditingViewport(snapshot, e))
            {
                SyncScrollViewerToSnapshot(snapshot);
                return;
            }

            if (snapshot != null &&
                Math.Abs(snapshot.ViewportState.HorizontalOffset - e.HorizontalOffset) < 0.1d &&
                Math.Abs(snapshot.ViewportState.VerticalOffset - e.VerticalOffset) < 0.1d)
            {
                return;
            }

            var universalArgs = WpfUniversalInputAdapter.CreateScrollChangedEventArgs(e.HorizontalOffset, e.VerticalOffset);
            _coordinator.ProcessInput(_coreInputAdapter.CreateScrollChangedInput(universalArgs, DateTime.UtcNow));
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_coordinator == null)
            {
                return;
            }

            SyncViewportSize();
            RenderViewportTrackMarkers(CurrentSnapshot);
            NotifyHostGeometryChanged();
        }

        private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_coordinator == null)
            {
                return;
            }

            SyncViewportSize();
            RenderViewportTrackMarkers(CurrentSnapshot);
            NotifyHostGeometryChanged();
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (IsHorizontalScrollBarInputSource(originalSource))
            {
                _isHorizontalScrollbarInteractionActive = true;
            }

            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    EndPointerCaptureSession();
                }

                return;
            }

            var position = ResolveMousePosition(e, originalSource);
            var leftButton = e.ChangedButton == MouseButton.Left || IsMouseLeftCaptureActive
                ? MouseButtonState.Pressed
                : e.LeftButton;
            var input = WpfUniversalInputAdapter.CreateMousePointerPressedEventArgs(
                position,
                e.ChangedButton,
                e.ClickCount,
                Keyboard.Modifiers,
                leftButton,
                e.RightButton,
                e.MiddleButton);

            e.Handled = HandlePointerPressed(input);
            if (e.Handled && e.ChangedButton == MouseButton.Left)
            {
                BeginMousePointerCapture(position);
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            var position = ResolveMousePosition(e, originalSource);
            UpdateCapturedPointerPosition(position);
            var input = WpfUniversalInputAdapter.CreateMousePointerMovedEventArgs(
                position,
                Keyboard.Modifiers,
                IsMouseLeftCaptureActive ? MouseButtonState.Pressed : e.LeftButton,
                e.RightButton,
                e.MiddleButton);

            UpdatePointerCursor(position);
            e.Handled = HandlePointerMoved(input);
        }

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (e.ChangedButton == MouseButton.Left)
            {
                _isHorizontalScrollbarInteractionActive = false;
            }

            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    EndPointerCaptureSession();
                }

                return;
            }

            var position = ResolveMousePosition(e, originalSource);
            UpdateCapturedPointerPosition(position);
            var input = WpfUniversalInputAdapter.CreateMousePointerReleasedEventArgs(
                position,
                e.ChangedButton,
                Keyboard.Modifiers,
                e.LeftButton,
                e.RightButton,
                e.MiddleButton);

            e.Handled = HandlePointerReleased(input);
            if (e.ChangedButton == MouseButton.Left)
            {
                EndPointerCaptureSession();
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            var input = WpfUniversalInputAdapter.CreateWheelEventArgs(
                e.Delta,
                ResolveMousePosition(e, originalSource),
                Keyboard.Modifiers);

            e.Handled = HandleWheel(input);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (IsCellEditorInputSource(originalSource))
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Tab)
                {
                    var editorInput = WpfUniversalInputAdapter.CreateKeyEventArgs(e.Key, true, Keyboard.Modifiers, e.IsRepeat);
                    e.Handled = HandleKey(editorInput);
                }

                return;
            }

            if (ShouldBypassSurfaceInput(originalSource))
            {
                return;
            }

            var input = WpfUniversalInputAdapter.CreateKeyEventArgs(e.Key, true, Keyboard.Modifiers, e.IsRepeat);
            e.Handled = HandleKey(input);
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            var input = WpfUniversalInputAdapter.CreateTextEventArgs(e.Text, Keyboard.Modifiers);
            e.Handled = HandleText(input);
        }

        private void OnPreviewTouchDown(object sender, TouchEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (IsHorizontalScrollBarInputSource(originalSource))
            {
                _isHorizontalScrollbarInteractionActive = true;
            }

            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                EndPointerCaptureSession();
                return;
            }

            var position = ResolveTouchPosition(e);
            var input = WpfUniversalInputAdapter.CreateTouchPointerPressedEventArgs(
                position,
                e.TouchDevice.Id,
                Keyboard.Modifiers);

            e.Handled = HandlePointerPressed(input);
            if (e.Handled)
            {
                BeginTouchPointerCapture(e.TouchDevice, position);
            }
        }

        private void OnPreviewTouchMove(object sender, TouchEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            var position = ResolveTouchPosition(e);
            var input = WpfUniversalInputAdapter.CreateTouchPointerMovedEventArgs(
                position,
                e.TouchDevice.Id,
                Keyboard.Modifiers);

            UpdateCapturedPointerPosition(position);
            e.Handled = HandlePointerMoved(input);
        }

        private void OnPreviewTouchUp(object sender, TouchEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            _isHorizontalScrollbarInteractionActive = false;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                EndPointerCaptureSession();
                return;
            }

            var position = ResolveTouchPosition(e);
            var input = WpfUniversalInputAdapter.CreateTouchPointerReleasedEventArgs(
                position,
                e.TouchDevice.Id,
                Keyboard.Modifiers);

            UpdateCapturedPointerPosition(position);
            e.Handled = HandlePointerReleased(input);
            EndPointerCaptureSession();
        }

        private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            CancelPointerCaptureSession(UniversalPointerCancelReason.ManipulationStarted);
            var input = WpfUniversalInputAdapter.CreateManipulationStartedEventArgs(
                e.ManipulationOrigin,
                Keyboard.Modifiers);

            e.Handled = HandleManipulationStarted(input);
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            var input = WpfUniversalInputAdapter.CreateManipulationDeltaEventArgs(
                e.ManipulationOrigin,
                e.DeltaManipulation,
                e.CumulativeManipulation,
                Keyboard.Modifiers);

            e.Handled = HandleManipulationDelta(input);
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (ShouldBypassSurfaceInput(originalSource) || IsCellEditorInputSource(originalSource))
            {
                return;
            }

            var input = WpfUniversalInputAdapter.CreateManipulationCompletedEventArgs(
                e.ManipulationOrigin,
                e.TotalManipulation,
                e.IsInertial,
                e.FinalVelocities,
                Keyboard.Modifiers);

            e.Handled = HandleManipulationCompleted(input);
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            HandleFocusChanged(WpfUniversalInputAdapter.CreateFocusChangedEventArgs(true, Keyboard.Modifiers));
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            HandleFocusChanged(WpfUniversalInputAdapter.CreateFocusChangedEventArgs(false, Keyboard.Modifiers));
            _isHorizontalScrollbarInteractionActive = false;
            CancelPointerCaptureSession(UniversalPointerCancelReason.FocusLost);
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            _isHorizontalScrollbarInteractionActive = false;
            if (_isChangingPointerCapture || _activePointerCapture == null || _activePointerCapture.DeviceType != DeviceType.Mouse)
            {
                return;
            }

            CancelPointerCaptureSession(UniversalPointerCancelReason.CaptureLost);
        }

        private void OnLostTouchCapture(object sender, TouchEventArgs e)
        {
            _isHorizontalScrollbarInteractionActive = false;
            if (_isChangingPointerCapture || _activePointerCapture == null || _activePointerCapture.DeviceType != DeviceType.Touch)
            {
                return;
            }

            if (_activePointerCapture.PointerId != unchecked((uint)e.TouchDevice.Id))
            {
                return;
            }

            CancelPointerCaptureSession(UniversalPointerCancelReason.CaptureLost);
        }

        private void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (e == null || !ShouldSuppressEditingBringIntoView(e))
            {
                return;
            }

            e.Handled = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachWindowMessageHook();
            _isHorizontalScrollbarInteractionActive = false;
            CancelPointerCaptureSession(UniversalPointerCancelReason.Unloaded);
        }

        private void AttachWindowMessageHook()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (ReferenceEquals(_hwndSource, hwndSource))
            {
                return;
            }

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(OnWindowMessage);
            }

            _hwndSource = hwndSource;
            _hwndSource?.AddHook(OnWindowMessage);
        }

        private void DetachWindowMessageHook()
        {
            if (_hwndSource == null)
            {
                return;
            }

            _hwndSource.RemoveHook(OnWindowMessage);
            _hwndSource = null;
        }

        private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (handled || msg != WmLButtonDown)
            {
                return IntPtr.Zero;
            }

            if (!TryGetCursorScreenPoint(out var screenPoint))
            {
                return IntPtr.Zero;
            }

            if (!TryHandleSelectionCheckboxAtScreenPoint(screenPoint))
            {
                return IntPtr.Zero;
            }

            Focus();
            Keyboard.Focus(this);
            handled = true;
            return IntPtr.Zero;
        }

        private void UpdatePointerCursor(Point position)
        {
            UpdatePointerCursor(position, GridHitTestSurfaceScope.DataSurface);
        }

        private void UpdatePointerCursor(Point position, GridHitTestSurfaceScope surfaceScope)
        {
            if (_activePointerCapture != null)
            {
                return;
            }

            var snapshot = CurrentSnapshot;
            if (snapshot == null)
            {
                Cursor = Cursors.Arrow;
                return;
            }

            var hit = _hitTesting.HitTest(position.X, position.Y, snapshot, surfaceScope);
            if (hit?.TargetKind == GridHitTargetKind.ColumnResizeHandle)
            {
                Cursor = Cursors.SizeWE;
                return;
            }

            if (hit?.TargetKind == GridHitTargetKind.Header &&
                hit.HeaderKind == GridHeaderKind.ColumnHeader)
            {
                Cursor = Cursors.Hand;
                return;
            }

            Cursor = Cursors.Arrow;
        }

        private void SyncViewportSize()
        {
            if (_coordinator == null)
            {
                return;
            }

            var width = ScrollViewer.ActualWidth;
            var height = ScrollViewer.ActualHeight;
            if (width <= 0d || height <= 0d)
            {
                return;
            }

            var universalArgs = WpfUniversalInputAdapter.CreateViewportChangedEventArgs(width, height);
            _coordinator.ProcessInput(_coreInputAdapter.CreateViewportChangedInput(universalArgs, DateTime.UtcNow));
        }

        private void RenderCurrentSnapshot()
        {
            var snapshot = _coordinator?.GetCurrentSnapshot();
            if (snapshot != null)
            {
                SurfacePanel.RenderSnapshot(snapshot);
                SyncScrollViewerToSnapshot(snapshot);
                RenderViewportTrackMarkers(snapshot);
                NotifyHostGeometryChanged();
            }
        }

        private void NotifyHostGeometryChanged()
        {
            HostGeometryChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SyncScrollViewerToSnapshot(GridSurfaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (Math.Abs(ScrollViewer.HorizontalOffset - snapshot.ViewportState.HorizontalOffset) < 0.1d &&
                Math.Abs(ScrollViewer.VerticalOffset - snapshot.ViewportState.VerticalOffset) < 0.1d)
            {
                return;
            }

            _isApplyingScrollState = true;
            try
            {
                ScrollViewer.ScrollToHorizontalOffset(snapshot.ViewportState.HorizontalOffset);
                ScrollViewer.ScrollToVerticalOffset(snapshot.ViewportState.VerticalOffset);
            }
            finally
            {
                _isApplyingScrollState = false;
            }
        }

        private void RenderViewportTrackMarkers(GridSurfaceSnapshot snapshot)
        {
            VerticalTrackMarkerCanvas.Children.Clear();

            if (snapshot?.ViewportState?.VerticalTrackMarkers == null ||
                snapshot.ViewportState.VerticalTrackMarkers.Count == 0 ||
                snapshot.ViewportState.MaxVerticalOffset <= 0.1d)
            {
                VerticalTrackMarkerHost.Visibility = Visibility.Collapsed;
                return;
            }

            var topOffset = Math.Max(0d, snapshot.ViewportState.DataTopInset);
            var bottomOffset = ScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible
                ? SystemParameters.HorizontalScrollBarHeight
                : 0d;
            var trackHeight = Math.Max(0d, ActualHeight - topOffset - bottomOffset);
            if (trackHeight <= 1d)
            {
                VerticalTrackMarkerHost.Visibility = Visibility.Collapsed;
                return;
            }

            VerticalTrackMarkerHost.Visibility = Visibility.Visible;
            VerticalTrackMarkerHost.Margin = new Thickness(0, topOffset, 2, bottomOffset);
            VerticalTrackMarkerCanvas.Height = trackHeight;
            VerticalTrackMarkerCanvas.Width = VerticalTrackMarkerHost.Width;
            const double topPadding = 2d;
            var usableTrackHeight = Math.Max(0d, trackHeight - (topPadding * 2d));

            foreach (var marker in snapshot.ViewportState.VerticalTrackMarkers)
            {
                var markerTop = topPadding + Math.Max(0d, Math.Min(usableTrackHeight, marker.StartRatio * usableTrackHeight));
                var markerBottom = topPadding + Math.Max(markerTop - topPadding, Math.Min(usableTrackHeight, marker.EndRatio * usableTrackHeight));
                var markerHeight = Math.Max(4d, markerBottom - markerTop);
                if (markerTop + markerHeight > trackHeight)
                {
                    markerTop = Math.Max(0d, trackHeight - markerHeight);
                }

                var glyph = new Border
                {
                    Width = 4d,
                    Height = markerHeight,
                    CornerRadius = new CornerRadius(2d),
                    ToolTip = string.IsNullOrWhiteSpace(marker.ToolTip) ? null : marker.ToolTip,
                };
                glyph.SetResourceReference(Border.BackgroundProperty, ResolveTrackMarkerBrushKey(marker.Kind));
                AutomationProperties.SetAutomationId(glyph, "surface.viewport-marker." + marker.Kind + "." + marker.TargetKey);
                Canvas.SetLeft(glyph, ResolveTrackMarkerLeft(marker.Kind));
                Canvas.SetTop(glyph, markerTop);
                VerticalTrackMarkerCanvas.Children.Add(glyph);
            }
        }

        private static double ResolveTrackMarkerLeft(GridViewportTrackMarkerKind kind)
        {
            switch (kind)
            {
                case GridViewportTrackMarkerKind.ValidationError:
                    return 6d;
                default:
                    return 1d;
            }
        }

        private static string ResolveTrackMarkerBrushKey(GridViewportTrackMarkerKind kind)
        {
            switch (kind)
            {
                case GridViewportTrackMarkerKind.ValidationError:
                    return "Brush.Danger.Border";
                default:
                    return "Brush.Warning.Border";
            }
        }

        private static bool ShouldBypassSurfaceInput(DependencyObject source)
        {
            return FindAncestor<Presenters.GridMasterDetailPresenter>(source) != null ||
                FindAncestor<Presenters.GridRowDetailPresenter>(source) != null ||
                IsRowIndicatorInputSource(source) ||
                FindAncestor<ScrollBar>(source) != null ||
                FindAncestor<Thumb>(source) != null ||
                FindAncestor<Track>(source) != null ||
                FindAncestor<RepeatButton>(source) != null;
        }

        private static bool IsRowIndicatorInputSource(DependencyObject source)
        {
            while (source != null)
            {
                if (source is FrameworkElement element)
                {
                    var automationId = AutomationProperties.GetAutomationId(element);
                    if (!string.IsNullOrWhiteSpace(automationId) &&
                        automationId.StartsWith("surface.row-indicator.", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private static bool IsCellEditorInputSource(DependencyObject source)
        {
            if (GridCellEditorInputScope.IsWithinEditorOwnedPopup(source))
            {
                return true;
            }

            return FindAncestor<TextBoxBase>(source) != null ||
                FindAncestor<ComboBox>(source) != null ||
                FindAncestor<DatePicker>(source) != null ||
                FindAncestor<Calendar>(source) != null ||
                FindAncestor<ToggleButton>(source) != null;
        }

        private static bool IsHorizontalScrollBarInputSource(DependencyObject source)
        {
            return FindAncestor<ScrollBar>(source) is ScrollBar scrollBar &&
                scrollBar.Orientation == Orientation.Horizontal;
        }

        private bool ShouldPreserveEditingViewport(GridSurfaceSnapshot snapshot, ScrollChangedEventArgs e)
        {
            if (snapshot?.ViewportState.IsInEditMode != true || e == null)
            {
                return false;
            }

            if (Math.Abs(snapshot.ViewportState.HorizontalOffset - e.HorizontalOffset) < 0.1d)
            {
                return false;
            }

            return !_isHorizontalScrollbarInteractionActive;
        }

        private bool ShouldSuppressEditingBringIntoView(RequestBringIntoViewEventArgs e)
        {
            var snapshot = CurrentSnapshot;
            if (snapshot?.ViewportState.IsInEditMode != true)
            {
                return false;
            }

            var target = e.OriginalSource as DependencyObject
                ?? e.TargetObject
                ?? e.Source as DependencyObject;
            if (target == null || !IsCellEditorInputSource(target))
            {
                return false;
            }

            return IsDescendantOfSurfaceHost(target);
        }

        private bool ShouldFocusEditingCellEditor(GridSurfaceSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.ViewportState.IsInEditMode)
            {
                return false;
            }

            var editingCellKey = GetEditingCellKey(snapshot);
            if (!_wasInEditMode || !string.Equals(_lastFocusedEditingCellKey, editingCellKey, StringComparison.Ordinal))
            {
                return true;
            }

            var focusedElement = Keyboard.FocusedElement as DependencyObject;
            return !IsCellEditorInputSource(focusedElement) || !IsDescendantOfSurfaceHost(focusedElement);
        }

        private bool IsDescendantOfSurfaceHost(DependencyObject candidate)
        {
            while (candidate != null)
            {
                if (ReferenceEquals(candidate, this))
                {
                    return true;
                }

                candidate = VisualTreeHelper.GetParent(candidate);
            }

            return false;
        }

        private static string GetEditingCellKey(GridSurfaceSnapshot snapshot)
        {
            if (snapshot?.CurrentCell == null)
            {
                return string.Empty;
            }

            return snapshot.CurrentCell.RowKey + "_" + snapshot.CurrentCell.ColumnKey;
        }

        private bool TryHandleSelectionCheckboxAtScreenPoint(Point screenPoint)
        {
            if (_coordinator == null || _activePointerCapture != null)
            {
                return false;
            }

            var snapshot = CurrentSnapshot;
            if (snapshot == null)
            {
                return false;
            }

            var target = (FrameworkElement)(ScrollViewer ?? (FrameworkElement)SurfacePanel);
            var point = target.PointFromScreen(screenPoint);
            var viewportBounds = new Rect(new Point(0d, 0d), target.RenderSize);
            if (!viewportBounds.Contains(point))
            {
                return false;
            }

            var hit = _hitTesting.HitTest(point.X, point.Y, snapshot, GridHitTestSurfaceScope.DataSurface);
            if (hit?.TargetKind != GridHitTargetKind.SelectionCheckbox || string.IsNullOrWhiteSpace(hit.RowKey))
            {
                return false;
            }

            var input = WpfUniversalInputAdapter.CreateMousePointerPressedEventArgs(
                point,
                MouseButton.Left,
                clickCount: 1,
                Keyboard.Modifiers,
                MouseButtonState.Pressed,
                MouseButtonState.Released,
                MouseButtonState.Released);

            return HandlePointerPressed(input);
        }

        private Point ResolveMousePosition(MouseEventArgs args, DependencyObject originalSource)
        {
            var relativeTo = (IInputElement)ScrollViewer ?? SurfacePanel;
            return (PointerPositionResolver ?? GridSurfacePointerPositionResolver.Default)
                .ResolvePosition(args, relativeTo, originalSource);
        }

        private Point ResolveTouchPosition(TouchEventArgs args)
        {
            if (args == null)
            {
                return new Point();
            }

            var relativeTo = (IInputElement)ScrollViewer ?? SurfacePanel;
            return args.GetTouchPoint(relativeTo).Position;
        }

        private static T FindAncestor<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T match)
                {
                    return match;
                }

                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private double ResolveScrollBarThickness(Orientation orientation, double fallback)
        {
            var scrollBar = FindDescendant<ScrollBar>(
                ScrollViewer,
                candidate => candidate.Orientation == orientation &&
                             candidate.Visibility == Visibility.Visible);
            if (scrollBar == null)
            {
                return fallback;
            }

            var thickness = orientation == Orientation.Vertical
                ? scrollBar.ActualWidth
                : scrollBar.ActualHeight;
            if (thickness <= 0d)
            {
                thickness = orientation == Orientation.Vertical
                    ? scrollBar.Width
                    : scrollBar.Height;
            }

            return thickness > 0d ? thickness : fallback;
        }

        private static T FindDescendant<T>(DependencyObject root, Func<T, bool> predicate)
            where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed && (predicate == null || predicate(typed)))
                {
                    return typed;
                }

                var nested = FindDescendant(child, predicate);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static bool TryGetCursorScreenPoint(out Point point)
        {
            if (GetCursorPos(out var cursorPoint))
            {
                point = new Point(cursorPoint.X, cursorPoint.Y);
                return true;
            }

            point = default;
            return false;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out NativePoint point);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridSurfaceHostAutomationPeer(this);
        }

        private sealed class GridSurfaceHostAutomationPeer : FrameworkElementAutomationPeer
        {
            public GridSurfaceHostAutomationPeer(GridSurfaceHost owner)
                : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.DataGrid;
            }

            protected override string GetClassNameCore()
            {
                return nameof(GridSurfaceHost);
            }

            protected override string GetNameCore()
            {
                var owner = (GridSurfaceHost)Owner;
                return AutomationProperties.GetName(owner) ?? base.GetNameCore();
            }
        }

        private bool HandlePointerPressed(UniversalPointerRoutedEventArgs args)
        {
            return HandlePointerPressed(args, GridHitTestSurfaceScope.DataSurface);
        }

        private bool HandlePointerPressed(UniversalPointerRoutedEventArgs args, GridHitTestSurfaceScope surfaceScope)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreatePointerPressedInput(args, DateTime.UtcNow), surfaceScope);
            return true;
        }

        private bool HandlePointerMoved(UniversalPointerRoutedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreatePointerMovedInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandlePointerReleased(UniversalPointerRoutedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreatePointerReleasedInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandlePointerCanceled(UniversalPointerCanceledEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreatePointerCanceledInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandleWheel(UniversalPointerWheelChangedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreateWheelInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandleKey(UniversalKeyEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            var input = _coreInputAdapter.CreateKeyInput(args, DateTime.UtcNow);
            if (input.Key == GridKey.Unknown)
            {
                return false;
            }

            _coordinator.ProcessInput(input);
            return true;
        }

        private bool HandleText(UniversalTextChangedEventArgs args)
        {
            if (_coordinator == null || args == null || string.IsNullOrEmpty(args.Text))
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreateTextInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandleFocusChanged(UniversalFocusChangedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreateFocusInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandleManipulationStarted(UniversalManipulationStartedRoutedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreateManipulationStartedInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandleManipulationDelta(UniversalManipulationDeltaRoutedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreateManipulationDeltaInput(args, DateTime.UtcNow));
            return true;
        }

        private bool HandleManipulationCompleted(UniversalManipulationCompletedRoutedEventArgs args)
        {
            if (_coordinator == null)
            {
                return false;
            }

            _coordinator.ProcessInput(_coreInputAdapter.CreateManipulationCompletedInput(args, DateTime.UtcNow));
            return true;
        }

        private bool IsMouseLeftCaptureActive =>
            _activePointerCapture?.DeviceType == DeviceType.Mouse &&
            _activePointerCapture.Button == UniversalPointerButton.Left;

        private void BeginMousePointerCapture(Point position)
        {
            CancelPointerCaptureSession(UniversalPointerCancelReason.PlatformCanceled);
            if (!(PointerCaptureController ?? GridSurfacePointerCaptureController.Default).TryCaptureMouse(this))
            {
                return;
            }

            _activePointerCapture = PlatformPointerCaptureSession.ForMouse(position);
        }

        private void BeginTouchPointerCapture(TouchDevice touchDevice, Point position)
        {
            CancelPointerCaptureSession(UniversalPointerCancelReason.PlatformCanceled);
            if (!(PointerCaptureController ?? GridSurfacePointerCaptureController.Default).TryCaptureTouch(this, touchDevice))
            {
                return;
            }

            _activePointerCapture = PlatformPointerCaptureSession.ForTouch(unchecked((uint)touchDevice.Id), touchDevice, position);
        }

        private void UpdateCapturedPointerPosition(Point position)
        {
            if (_activePointerCapture == null)
            {
                return;
            }

            _activePointerCapture.LastPosition = position;
        }

        private void CancelPointerCaptureSession(UniversalPointerCancelReason reason)
        {
            var session = _activePointerCapture;
            if (session == null)
            {
                return;
            }

            _activePointerCapture = null;
            ReleasePointerCapture(session);
            HandlePointerCanceled(WpfUniversalInputAdapter.CreatePointerCanceledEventArgs(
                session.DeviceType,
                session.PointerId,
                session.LastPosition,
                Keyboard.Modifiers,
                reason));
        }

        private void EndPointerCaptureSession()
        {
            var session = _activePointerCapture;
            if (session == null)
            {
                return;
            }

            _activePointerCapture = null;
            ReleasePointerCapture(session);
        }

        private void ReleasePointerCapture(PlatformPointerCaptureSession session)
        {
            _isChangingPointerCapture = true;
            try
            {
                if (session.DeviceType == DeviceType.Mouse)
                {
                    (PointerCaptureController ?? GridSurfacePointerCaptureController.Default).ReleaseMouse(this);
                    return;
                }

                (PointerCaptureController ?? GridSurfacePointerCaptureController.Default).ReleaseTouch(session.TouchDevice);
            }
            finally
            {
                _isChangingPointerCapture = false;
            }
        }

        private sealed class PlatformPointerCaptureSession
        {
            private PlatformPointerCaptureSession(DeviceType deviceType, uint pointerId, UniversalPointerButton button, Point lastPosition, TouchDevice touchDevice)
            {
                DeviceType = deviceType;
                PointerId = pointerId;
                Button = button;
                LastPosition = lastPosition;
                TouchDevice = touchDevice;
            }

            public DeviceType DeviceType { get; }

            public uint PointerId { get; }

            public UniversalPointerButton Button { get; }

            public Point LastPosition { get; set; }

            public TouchDevice TouchDevice { get; }

            public static PlatformPointerCaptureSession ForMouse(Point position)
            {
                return new PlatformPointerCaptureSession(DeviceType.Mouse, 0, UniversalPointerButton.Left, position, touchDevice: null);
            }

            public static PlatformPointerCaptureSession ForTouch(uint pointerId, TouchDevice touchDevice, Point position)
            {
                return new PlatformPointerCaptureSession(DeviceType.Touch, pointerId, UniversalPointerButton.Left, position, touchDevice);
            }
        }
    }
}
