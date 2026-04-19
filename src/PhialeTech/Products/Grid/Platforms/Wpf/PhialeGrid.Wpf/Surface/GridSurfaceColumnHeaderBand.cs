using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhialeGrid.Core.Surface;
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

        public GridSurfaceColumnHeaderBand()
        {
            ClipToBounds = true;
            SnapsToDevicePixels = true;
            _containerPool.RegisterFactory("column-header", () => new GridColumnHeaderPresenter());
        }

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

            var rowHeaderWidth = snapshot?.ViewportState?.RowHeaderWidth ?? 0d;
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
                    Math.Max(0d, header.Bounds.X - rowHeaderWidth),
                    0d,
                    header.Bounds.Width,
                    header.Bounds.Height);
            }

            Width = Math.Max(0d, (snapshot?.ViewportState?.ViewportWidth ?? 0d) - rowHeaderWidth);
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
    }
}
