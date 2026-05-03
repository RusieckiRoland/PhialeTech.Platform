using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Pools;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    /// <summary>
    /// Panel do renderowania komórek grida.
    /// Przyjmuje GridSurfaceSnapshot i rysuje wszystkie elementy na Canvas'ie.
    /// To jest prosty adapter - logika jest w Core.
    /// </summary>
    public sealed class GridSurfacePanel : Canvas
    {
        private const string CellContainerType = "cell";
        private const string ColumnHeaderContainerType = "column-header";
        private const string RowHeaderContainerType = "row-header";
        private const string OverlayContainerType = "overlay";
        private const string MasterDetailOverlayContainerType = "master-detail-overlay";

        private readonly GridContainerPool _containerPool = new GridContainerPool();
        private readonly Dictionary<string, RenderedItem> _renderedItems =
            new Dictionary<string, RenderedItem>(StringComparer.Ordinal);
        private readonly Canvas _headerLayer = new Canvas();
        private readonly Canvas _cellLayer = new Canvas();
        private readonly Canvas _overlayLayer = new Canvas();

        public event EventHandler<GridCellEditingTextChangedEventArgs> CellEditingTextChanged;

        public GridSurfacePanel()
        {
            SetResourceReference(BackgroundProperty, "PgGridBackgroundBrush");
            SnapsToDevicePixels = true;

            Panel.SetZIndex(_cellLayer, 0);
            Panel.SetZIndex(_headerLayer, 10);
            Panel.SetZIndex(_overlayLayer, 20);

            Children.Add(_headerLayer);
            Children.Add(_cellLayer);
            Children.Add(_overlayLayer);

            _containerPool.RegisterFactory(CellContainerType, () => new GridCellPresenter());
            _containerPool.RegisterFactory(ColumnHeaderContainerType, () => new GridColumnHeaderPresenter());
            _containerPool.RegisterFactory(RowHeaderContainerType, () => new GridRowHeaderPresenter());
            _containerPool.RegisterFactory(OverlayContainerType, () => new GridOverlayPresenter());
            _containerPool.RegisterFactory(MasterDetailOverlayContainerType, () => new GridMasterDetailPresenter());
        }

        /// <summary>
        /// Renderuje snapshot na panelu.
        /// </summary>
        public void RenderSnapshot(GridSurfaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var desiredItems = new List<RenderDescriptor>();
            if (snapshot.Headers != null)
            {
                foreach (var header in snapshot.Headers)
                {
                    if (header.Kind == GridHeaderKind.RowHeader || header.Kind == GridHeaderKind.RowNumberHeader)
                    {
                        desiredItems.Add(new RenderDescriptor(header.ItemKey, RowHeaderContainerType, _headerLayer, header));
                    }
                }
            }

            if (snapshot.Cells != null)
            {
                foreach (var cell in snapshot.Cells)
                {
                    if (!cell.IsDummy)
                    {
                        desiredItems.Add(new RenderDescriptor(cell.ItemKey, CellContainerType, _cellLayer, cell));
                    }
                }
            }

            if (snapshot.Overlays != null)
            {
                foreach (var overlay in snapshot.Overlays)
                {
                    desiredItems.Add(new RenderDescriptor(
                        overlay.ItemKey,
                        overlay.Payload is global::PhialeTech.PhialeGrid.Wpf.Controls.GridMasterDetailMasterRowModel ? MasterDetailOverlayContainerType : OverlayContainerType,
                        _overlayLayer,
                        overlay));
                }
            }

            var desiredKeys = new HashSet<string>(desiredItems.Select(item => item.ItemKey), StringComparer.Ordinal);
            foreach (var obsoleteKey in _renderedItems.Keys.Where(key => !desiredKeys.Contains(key)).ToArray())
            {
                ReleaseRenderedItem(obsoleteKey);
            }

            foreach (var item in desiredItems)
            {
                RealizeOrUpdate(item);
            }

            RenderTransform = new TranslateTransform(
                snapshot.ViewportState.HorizontalOffset,
                snapshot.ViewportState.VerticalOffset);
            Width = Math.Max(ActualWidth, snapshot.ViewportState.TotalWidth);
            Height = Math.Max(ActualHeight, snapshot.ViewportState.TotalHeight);

            _headerLayer.Width = Width;
            _headerLayer.Height = Height;
            _cellLayer.Width = Width;
            _cellLayer.Height = Height;
            _overlayLayer.Width = Width;
            _overlayLayer.Height = Height;
        }

        internal void FocusEditingCellEditor()
        {
            foreach (var renderedItem in _renderedItems.Values)
            {
                if (renderedItem.Element is GridCellPresenter cellPresenter &&
                    cellPresenter.CellData?.IsEditing == true)
                {
                    cellPresenter.FocusEditor();
                    return;
                }
            }
        }

        private void RealizeOrUpdate(RenderDescriptor descriptor)
        {
            if (!_renderedItems.TryGetValue(descriptor.ItemKey, out var renderedItem))
            {
                var element = (FrameworkElement)_containerPool.AcquireContainer(descriptor.ContainerType);
                if (element is GridCellPresenter cellPresenter)
                {
                    cellPresenter.EditingTextChanged -= HandleCellPresenterEditingTextChanged;
                    cellPresenter.EditingTextChanged += HandleCellPresenterEditingTextChanged;
                }

                descriptor.Layer.Children.Add(element);
                renderedItem = new RenderedItem(descriptor.ContainerType, descriptor.Layer, element);
                _renderedItems[descriptor.ItemKey] = renderedItem;
            }

            if (!ReferenceEquals(renderedItem.Layer, descriptor.Layer))
            {
                renderedItem.Layer.Children.Remove(renderedItem.Element);
                descriptor.Layer.Children.Add(renderedItem.Element);
                renderedItem.Layer = descriptor.Layer;
            }

            BindDescriptor(renderedItem.Element, descriptor.Item);
        }

        private void BindDescriptor(FrameworkElement element, object item)
        {
            if (element is GridCellPresenter cellPresenter && item is GridCellSurfaceItem cell)
            {
                if (AreEquivalent(cellPresenter.CellData, cell))
                {
                    return;
                }

                cellPresenter.CellData = cell;
                cellPresenter.Bounds = cell.Bounds;
                return;
            }

            if (element is GridColumnHeaderPresenter columnHeaderPresenter && item is GridHeaderSurfaceItem columnHeader)
            {
                columnHeaderPresenter.HeaderData = columnHeader;
                columnHeaderPresenter.Bounds = columnHeader.Bounds;
                return;
            }

            if (element is GridRowHeaderPresenter rowHeaderPresenter && item is GridHeaderSurfaceItem rowHeader)
            {
                rowHeaderPresenter.HeaderData = rowHeader;
                rowHeaderPresenter.Bounds = rowHeader.Bounds;
                return;
            }

            if (element is GridOverlayPresenter overlayPresenter && item is GridOverlaySurfaceItem overlay)
            {
                overlayPresenter.OverlayData = overlay;
                overlayPresenter.Bounds = overlay.Bounds;
                return;
            }

            if (element is GridMasterDetailPresenter masterDetailPresenter && item is GridOverlaySurfaceItem masterDetailOverlay)
            {
                masterDetailPresenter.OverlayData = masterDetailOverlay;
                masterDetailPresenter.Bounds = masterDetailOverlay.Bounds;
            }
        }

        private static bool AreEquivalent(GridCellSurfaceItem current, GridCellSurfaceItem next)
        {
            if (current == null || next == null)
            {
                return false;
            }

            return string.Equals(current.ItemKey, next.ItemKey, StringComparison.Ordinal) &&
                   string.Equals(current.RowKey, next.RowKey, StringComparison.Ordinal) &&
                   string.Equals(current.ColumnKey, next.ColumnKey, StringComparison.Ordinal) &&
                   current.Bounds.Equals(next.Bounds) &&
                   string.Equals(current.StyleKey, next.StyleKey, StringComparison.Ordinal) &&
                   current.RenderLayer == next.RenderLayer &&
                   string.Equals(current.DisplayText, next.DisplayText, StringComparison.Ordinal) &&
                   Equals(current.RawValue, next.RawValue) &&
                   string.Equals(current.ValueKind, next.ValueKind, StringComparison.Ordinal) &&
                   current.IsSelected == next.IsSelected &&
                   current.IsCurrent == next.IsCurrent &&
                   current.IsCurrentRow == next.IsCurrentRow &&
                   current.IsEditing == next.IsEditing &&
                   string.Equals(current.EditingText, next.EditingText, StringComparison.Ordinal) &&
                   current.IsReadOnly == next.IsReadOnly &&
                   current.HasValidationError == next.HasValidationError &&
                   current.DisplayState == next.DisplayState &&
                   current.ChangeState == next.ChangeState &&
                   current.ValidationState == next.ValidationState &&
                   current.AccessState == next.AccessState &&
                   string.Equals(current.EditSessionId, next.EditSessionId, StringComparison.Ordinal) &&
                   string.Equals(current.ValidationError, next.ValidationError, StringComparison.Ordinal) &&
                   current.IsFrozen == next.IsFrozen &&
                   string.Equals(current.ContentTemplateKey, next.ContentTemplateKey, StringComparison.Ordinal) &&
                   current.EditorKind == next.EditorKind &&
                   current.EditorItemsMode == next.EditorItemsMode &&
                   string.Equals(current.EditMask, next.EditMask, StringComparison.Ordinal) &&
                   current.IsDummy == next.IsDummy &&
                   current.IsGroupCaptionCell == next.IsGroupCaptionCell &&
                   current.ShowInlineChevron == next.ShowInlineChevron &&
                   current.IsInlineChevronExpanded == next.IsInlineChevronExpanded &&
                   current.ContentIndent.Equals(next.ContentIndent) &&
                   AreEquivalent(current.EditorItems, next.EditorItems);
        }

        private static bool AreEquivalent(IReadOnlyList<string> current, IReadOnlyList<string> next)
        {
            if (ReferenceEquals(current, next))
            {
                return true;
            }

            if (current == null || next == null || current.Count != next.Count)
            {
                return false;
            }

            for (var i = 0; i < current.Count; i++)
            {
                if (!string.Equals(current[i], next[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private void ReleaseRenderedItem(string itemKey)
        {
            if (!_renderedItems.TryGetValue(itemKey, out var renderedItem))
            {
                return;
            }

            renderedItem.Layer.Children.Remove(renderedItem.Element);
            _containerPool.ReleaseContainer(renderedItem.ContainerType, renderedItem.Element);
            _renderedItems.Remove(itemKey);
        }

        private void HandleCellPresenterEditingTextChanged(object sender, GridCellEditingTextChangedEventArgs e)
        {
            CellEditingTextChanged?.Invoke(this, e);
        }

        private sealed class RenderDescriptor
        {
            public RenderDescriptor(string itemKey, string containerType, Canvas layer, object item)
            {
                ItemKey = itemKey;
                ContainerType = containerType;
                Layer = layer;
                Item = item;
            }

            public string ItemKey { get; }

            public string ContainerType { get; }

            public Canvas Layer { get; }

            public object Item { get; }
        }

        private sealed class RenderedItem
        {
            public RenderedItem(string containerType, Canvas layer, FrameworkElement element)
            {
                ContainerType = containerType;
                Layer = layer;
                Element = element;
            }

            public string ContainerType { get; }

            public Canvas Layer { get; set; }

            public FrameworkElement Element { get; }
        }
    }
}
