using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.HitTesting
{
    /// <summary>
    /// Serwis do hit testing'u - określania, co się znajduje pod podaną pozycją w gridzie.
    /// </summary>
    public sealed class GridHitTestingService
    {
        /// <summary>
        /// Padding dla hit testing'u edgów (ile pixeli od krawędzi uważa się za edge).
        /// </summary>
        private const double EdgeHitPadding = 8.0;
        private const double ColumnResizeEdgeHitPadding = 4.0;
        private const double DetailsToggleWidth = 16.0;
        private const double HierarchyToggleWidth = 16.0;

        /// <summary>
        /// Przeprowadza hit testing na podanej pozycji.
        /// </summary>
        public GridHitTestResult HitTest(
            double x,
            double y,
            GridSurfaceSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            // Najpierw sprawdzam overlaye (ze względu na z-order)
            var overlayHit = HitTestOverlays(x, y, snapshot);
            if (overlayHit != null && overlayHit.TargetKind != GridHitTargetKind.None)
                return overlayHit;

            // Potem nagłówki
            var headerHit = HitTestHeaders(x, y, snapshot);
            if (headerHit != null && headerHit.TargetKind != GridHitTargetKind.None)
                return headerHit;

            // Potem current cell marker 
            var currentCellHit = HitTestCurrentCell(x, y, snapshot);
            if (currentCellHit != null && currentCellHit.TargetKind != GridHitTargetKind.None)
                return currentCellHit;

            // Potem komórki
            var cellHit = HitTestCells(x, y, snapshot);
            if (cellHit != null && cellHit.TargetKind != GridHitTargetKind.None)
                return cellHit;

            // Default: pusta przestrzeń
            return new GridHitTestResult(GridHitTargetKind.EmptySpace)
            {
                HitX = x,
                HitY = y,
            };
        }

        private GridHitTestResult HitTestOverlays(double x, double y, GridSurfaceSnapshot snapshot)
        {
            foreach (var overlay in snapshot.Overlays
                .OrderByDescending(candidate => candidate.DrawPriority)
                .ThenByDescending(candidate => candidate.RenderLayer)
                .ThenByDescending(candidate => candidate.SnapshotRevision))
            {
                if (!overlay.IsInteractive)
                {
                    continue;
                }

                if (overlay.Bounds.Contains(x, y))
                {
                    return new GridHitTestResult(GridHitTargetKind.Overlay)
                    {
                        OverlayKind = overlay.Kind,
                        TargetKey = overlay.OverlayKey,
                        Zone = GridHitZone.Interior,
                        IsInteractive = overlay.IsInteractive,
                        HitX = x,
                        HitY = y,
                    };
                }
            }
            return null;
        }

        private GridHitTestResult HitTestCurrentCell(double x, double y, GridSurfaceSnapshot snapshot)
        {
            if (snapshot.CurrentCell == null)
                return null;

            // Szukam overlay'u CurrentCell
            var currentCellOverlay = snapshot.Overlays.FirstOrDefault(
                o => o.Kind == GridOverlayKind.CurrentCell);

            if (currentCellOverlay != null && currentCellOverlay.Bounds.Contains(x, y))
            {
                return new GridHitTestResult(GridHitTargetKind.CurrentCellMarker)
                {
                    RowKey = snapshot.CurrentCell.RowKey,
                    ColumnKey = snapshot.CurrentCell.ColumnKey,
                    Zone = GridHitZone.Interior,
                    IsInteractive = false,
                    HitX = x,
                    HitY = y,
                };
            }
            return null;
        }

        private GridHitTestResult HitTestHeaders(double x, double y, GridSurfaceSnapshot snapshot)
        {
            // Szukam w headerach z dołu listy (bo są rysowane wcześniej)
            for (int i = snapshot.Headers.Count - 1; i >= 0; i--)
            {
                var header = snapshot.Headers[i];
                if (header.Bounds.Contains(x, y))
                {
                    var row = IsRowUtilityHeaderKind(header.Kind)
                        ? snapshot.Rows.FirstOrDefault(candidate => candidate.RowKey == header.HeaderKey)
                        : null;

                    if (TryCreateSpecialRowHeaderHit(x, y, header, row, out var rowHeaderHit))
                    {
                        return rowHeaderHit;
                    }

                    var zone = DetermineHeaderZone(x, y, header);
                    var targetKind = DetermineHeaderTargetKind(header, zone);
                    return new GridHitTestResult(targetKind)
                    {
                        HeaderKey = header.HeaderKey,
                        RowKey = row?.RowKey,
                        HeaderKind = header.Kind,
                        Zone = zone,
                        IsInteractive = true,
                        HitX = x,
                        HitY = y,
                    };
                }
            }
            return null;
        }

        private GridHitTestResult HitTestCells(double x, double y, GridSurfaceSnapshot snapshot)
        {
            // Szukam komóriki pod pozycją
            foreach (var cell in snapshot.Cells)
            {
                if (!cell.IsDummy && cell.Bounds.Contains(x, y))
                {
                    var zone = DetermineCellZone(x, y, cell);
                    return new GridHitTestResult(GridHitTargetKind.Cell)
                    {
                        RowKey = cell.RowKey,
                        ColumnKey = cell.ColumnKey,
                        Zone = zone,
                        IsInteractive = !cell.IsReadOnly,
                        HitX = x,
                        HitY = y,
                    };
                }
            }
            return null;
        }

        private GridHitZone DetermineHeaderZone(double x, double y, GridHeaderSurfaceItem header)
        {
            var bounds = header.Bounds;
            var dist_left = x - bounds.Left;
            var dist_right = bounds.Right - x;
            var dist_top = y - bounds.Top;
            var dist_bottom = bounds.Bottom - y;

            var horizontalEdgeHitPadding = header.Kind == GridHeaderKind.ColumnHeader
                ? ColumnResizeEdgeHitPadding
                : EdgeHitPadding;

            var min_horiz = Math.Min(dist_left, dist_right);
            var min_vert = Math.Min(dist_top, dist_bottom);

            // Jeśli blisko edge'u
            if (min_horiz < horizontalEdgeHitPadding || min_vert < EdgeHitPadding)
            {
                if (dist_left < horizontalEdgeHitPadding) return GridHitZone.LeftEdge;
                if (dist_right < horizontalEdgeHitPadding) return GridHitZone.RightEdge;
                if (dist_top < EdgeHitPadding) return GridHitZone.TopEdge;
                if (dist_bottom < EdgeHitPadding) return GridHitZone.BottomEdge;
            }

            return GridHitZone.Interior;
        }

        private GridHitTargetKind DetermineHeaderTargetKind(GridHeaderSurfaceItem header, GridHitZone zone)
        {
            if (header.IsResizable)
            {
                if (header.Kind == GridHeaderKind.ColumnHeader &&
                    (zone == GridHitZone.LeftEdge || zone == GridHitZone.RightEdge))
                {
                    return GridHitTargetKind.ColumnResizeHandle;
                }

                if (IsRowUtilityHeaderKind(header.Kind) &&
                    (zone == GridHitZone.TopEdge || zone == GridHitZone.BottomEdge))
                {
                    return GridHitTargetKind.RowResizeHandle;
                }
            }

            return GridHitTargetKind.Header;
        }

        private bool TryCreateSpecialRowHeaderHit(
            double x,
            double y,
            GridHeaderSurfaceItem header,
            GridRowSurfaceItem row,
            out GridHitTestResult result)
        {
            result = null;
            if (header.Kind != GridHeaderKind.RowHeader || row == null)
            {
                return false;
            }

            if (row.HasDetailsExpanded &&
                x >= header.Bounds.Left &&
                x <= header.Bounds.Left + DetailsToggleWidth)
            {
                result = new GridHitTestResult(GridHitTargetKind.Details)
                {
                    HeaderKey = header.HeaderKey,
                    RowKey = row.RowKey,
                    HeaderKind = header.Kind,
                    Zone = GridHitZone.Interior,
                    IsInteractive = true,
                    HitX = x,
                    HitY = y,
                };
                return true;
            }

            if (header.ShowSelectionCheckbox)
            {
                var checkboxStart = GetSelectionCheckboxStart(header);
                var checkboxEnd = checkboxStart + header.SelectionCheckboxWidth;
                if (x >= checkboxStart && x <= checkboxEnd)
                {
                    result = new GridHitTestResult(GridHitTargetKind.SelectionCheckbox)
                    {
                        HeaderKey = header.HeaderKey,
                        RowKey = row.RowKey,
                        HeaderKind = header.Kind,
                        Zone = GridHitZone.Interior,
                        IsInteractive = true,
                        HitX = x,
                        HitY = y,
                    };
                    return true;
                }
            }

            var hierarchyOffset = row.HasDetailsExpanded ? DetailsToggleWidth : 0;
            var hierarchyStart = header.Bounds.Left + hierarchyOffset + (row.HierarchyLevel * HierarchyToggleWidth);
            var hierarchyEnd = hierarchyStart + HierarchyToggleWidth;
            if (row.HasHierarchyChildren && x >= hierarchyStart && x <= hierarchyEnd)
            {
                result = new GridHitTestResult(GridHitTargetKind.HierarchyToggle)
                {
                    HeaderKey = header.HeaderKey,
                    RowKey = row.RowKey,
                    HeaderKind = header.Kind,
                    Zone = GridHitZone.Interior,
                    IsInteractive = true,
                    HitX = x,
                    HitY = y,
                };
                return true;
            }

            return false;
        }

        private static double GetSelectionCheckboxStart(GridHeaderSurfaceItem header)
        {
            var indicatorWidth = header.RowIndicatorWidth;
            return header.Bounds.Left + indicatorWidth;
        }

        private static bool IsRowUtilityHeaderKind(GridHeaderKind kind)
        {
            return kind == GridHeaderKind.RowHeader || kind == GridHeaderKind.RowNumberHeader;
        }

        private GridHitZone DetermineCellZone(double x, double y, GridCellSurfaceItem cell)
        {
            var bounds = cell.Bounds;
            var dist_left = x - bounds.Left;
            var dist_right = bounds.Right - x;
            var dist_top = y - bounds.Top;
            var dist_bottom = bounds.Bottom - y;

            // Dla komórek zwracam zawsze Interior (bo edges to resize handlery, które są overlayami)
            return GridHitZone.Interior;
        }
    }
}
