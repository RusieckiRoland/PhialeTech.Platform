using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Pools;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    /// <summary>
    /// WPF-only presenter for the integrated column header band.
    /// It renders column headers from the shared surface snapshot without owning header semantics.
    /// </summary>
    public sealed class GridSurfaceColumnHeaderBand : Canvas
    {
        private readonly GridContainerPool _containerPool = new GridContainerPool();
        private readonly Dictionary<string, GridColumnHeaderPresenter> _renderedHeaders =
            new Dictionary<string, GridColumnHeaderPresenter>(StringComparer.Ordinal);
        private double _rowHeaderWidth;
        private bool _isPointerInteractionActive;

        public GridSurfaceColumnHeaderBand()
        {
            Background = Brushes.Transparent;
            ClipToBounds = true;
            SnapsToDevicePixels = true;
            _containerPool.RegisterFactory("column-header", () => new GridColumnHeaderPresenter());
            AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(HandlePreviewMouseLeftButtonDown), true);
            AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(HandlePreviewMouseMove), true);
            AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler(HandlePreviewMouseLeftButtonUp), true);
            MouseLeave += HandleMouseLeave;
            LostMouseCapture += HandleLostMouseCapture;
        }

        public GridSurfaceHost InputSurfaceHost { get; set; }

        public void RenderSnapshot(GridSurfaceSnapshot snapshot)
        {
            var headers = snapshot?.Headers?
                .Where(header => header.Kind == GridHeaderKind.ColumnHeader)
                .ToArray() ?? Array.Empty<GridHeaderSurfaceItem>();

            var desiredKeys = new HashSet<string>(headers.Select(header => header.ItemKey), StringComparer.Ordinal);
            foreach (var obsoleteKey in _renderedHeaders.Keys.Where(key => !desiredKeys.Contains(key)).ToArray())
            {
                ReleaseHeader(obsoleteKey);
            }

            _rowHeaderWidth = snapshot?.ViewportState?.RowHeaderWidth ?? 0d;
            foreach (var header in headers)
            {
                if (!_renderedHeaders.TryGetValue(header.ItemKey, out var presenter))
                {
                    presenter = (GridColumnHeaderPresenter)_containerPool.AcquireContainer("column-header");
                    Children.Add(presenter);
                    _renderedHeaders[header.ItemKey] = presenter;
                }

                presenter.HeaderData = header;
                presenter.Bounds = new GridBounds(
                    Math.Max(0d, header.Bounds.X - _rowHeaderWidth),
                    0d,
                    header.Bounds.Width,
                    header.Bounds.Height);
            }

            Width = Math.Max(0d, (snapshot?.ViewportState?.ViewportWidth ?? 0d) - _rowHeaderWidth);
            Height = snapshot?.ViewportState?.ColumnHeaderHeight ?? 0d;
        }

        public void ClearSnapshot()
        {
            foreach (var key in _renderedHeaders.Keys.ToArray())
            {
                ReleaseHeader(key);
            }

            Width = 0d;
            Height = 0d;
            _rowHeaderWidth = 0d;
        }

        private void ReleaseHeader(string itemKey)
        {
            if (!_renderedHeaders.TryGetValue(itemKey, out var presenter))
            {
                return;
            }

            Children.Remove(presenter);
            _containerPool.ReleaseContainer("column-header", presenter);
            _renderedHeaders.Remove(itemKey);
        }

        private void HandlePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton != MouseButton.Left || InputSurfaceHost == null)
            {
                return;
            }

            var bandPosition = ResolveBandPointerPosition(e);
            if (!IsOverRenderedHeader(bandPosition))
            {
                return;
            }

            var position = ResolveSurfacePointerPosition(bandPosition);
            var input = WpfUniversalInputAdapter.CreateMousePointerPressedEventArgs(
                position,
                e.ChangedButton,
                e.ClickCount,
                Keyboard.Modifiers,
                e.LeftButton,
                e.RightButton,
                e.MiddleButton);

            if (!InputSurfaceHost.HandleExternalPointerPressed(input))
            {
                return;
            }

            _isPointerInteractionActive = true;
            CaptureMouse();
            InputSurfaceHost.Focus();
            Keyboard.Focus(InputSurfaceHost);
            e.Handled = true;
        }

        private void HandlePreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e == null || InputSurfaceHost == null)
            {
                return;
            }

            var bandPosition = ResolveBandPointerPosition(e);
            if (!_isPointerInteractionActive && !IsOverRenderedHeader(bandPosition))
            {
                return;
            }

            var position = ResolveSurfacePointerPosition(bandPosition);
            InputSurfaceHost.UpdateExternalPointerPosition(position);
            var leftButton = _isPointerInteractionActive
                ? MouseButtonState.Pressed
                : e.LeftButton;
            var input = WpfUniversalInputAdapter.CreateMousePointerMovedEventArgs(
                position,
                Keyboard.Modifiers,
                leftButton,
                e.RightButton,
                e.MiddleButton);

            if (InputSurfaceHost.HandleExternalPointerMoved(input))
            {
                Cursor = InputSurfaceHost.Cursor;
                if (_isPointerInteractionActive)
                {
                    e.Handled = true;
                }
            }
        }

        private void HandlePreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e == null ||
                e.ChangedButton != MouseButton.Left ||
                !_isPointerInteractionActive ||
                InputSurfaceHost == null)
            {
                return;
            }

            var position = ResolveSurfacePointerPosition(ResolveBandPointerPosition(e));
            InputSurfaceHost.UpdateExternalPointerPosition(position);
            var input = WpfUniversalInputAdapter.CreateMousePointerReleasedEventArgs(
                position,
                e.ChangedButton,
                Keyboard.Modifiers,
                e.LeftButton,
                e.RightButton,
                e.MiddleButton);

            InputSurfaceHost.HandleExternalPointerReleased(input);
            InputSurfaceHost.EndExternalPointerCapture();
            ReleasePointerInteraction();
            e.Handled = true;
        }

        private void HandleMouseLeave(object sender, MouseEventArgs e)
        {
            if (_isPointerInteractionActive)
            {
                return;
            }

            ClearValue(CursorProperty);
        }

        private void HandleLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (!_isPointerInteractionActive || InputSurfaceHost == null)
            {
                return;
            }

            InputSurfaceHost.EndExternalPointerCapture();
            ReleasePointerInteraction();
        }

        private Point ResolveSurfacePointerPosition(Point position)
        {
            return new Point(Math.Max(0d, _rowHeaderWidth + position.X), position.Y);
        }

        private Point ResolveBandPointerPosition(MouseEventArgs e)
        {
            if (e == null)
            {
                return new Point();
            }

            var originalSource = e.OriginalSource as DependencyObject;
            var resolver = InputSurfaceHost?.PointerPositionResolver ?? GridSurfacePointerPositionResolver.Default;
            return resolver.ResolvePosition(e, this, originalSource);
        }

        private void ReleasePointerInteraction()
        {
            _isPointerInteractionActive = false;
            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }

            ClearValue(CursorProperty);
        }

        private bool IsOverRenderedHeader(Point position)
        {
            return _renderedHeaders.Values.Any(presenter =>
            {
                var bounds = presenter.Bounds;
                return position.X >= bounds.Left &&
                    position.X <= bounds.Right &&
                    position.Y >= bounds.Top &&
                    position.Y <= bounds.Bottom;
            });
        }

        private static T FindAncestor<T>(DependencyObject source)
            where T : DependencyObject
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
    }
}
