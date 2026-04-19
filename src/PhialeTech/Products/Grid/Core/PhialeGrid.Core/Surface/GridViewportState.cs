namespace PhialeGrid.Core.Surface
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stan viewportu grida.
    /// Opisuje scroll position, rozmiar viewport'u i ustawienia virtulizacji.
    /// </summary>
    public sealed class GridViewportState
    {
        public GridViewportState(
            double horizontalOffset,
            double verticalOffset,
            double viewportWidth,
            double viewportHeight,
            GridViewportMetrics metrics)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            Metrics = metrics ?? throw new System.ArgumentNullException(nameof(metrics));
        }

        /// <summary>
        /// Horizontal scroll offset w pixelach.
        /// </summary>
        public double HorizontalOffset { get; set; }

        /// <summary>
        /// Vertical scroll offset w pixelach.
        /// </summary>
        public double VerticalOffset { get; set; }

        /// <summary>
        /// Szerokość viewport'u w pixelach.
        /// </summary>
        public double ViewportWidth { get; set; }

        /// <summary>
        /// Wysokość viewport'u w pixelach.
        /// </summary>
        public double ViewportHeight { get; set; }

        /// <summary>
        /// Metryki grida (wysokości wierszy, szerokości kolumn itp.).
        /// </summary>
        public GridViewportMetrics Metrics { get; }

        /// <summary>
        /// Buffer przed viewport'em (w pixelach).
        /// Wiersze/kolumny poza viewport'em ale wewnątrz buffer'a są virtulizowane.
        /// </summary>
        public double BufferBefore { get; set; } = 500;

        /// <summary>
        /// Buffer za viewport'em (w pixelach).
        /// </summary>
        public double BufferAfter { get; set; } = 500;

        /// <summary>
        /// Czy horizontal scroll jest włączony.
        /// </summary>
        public bool AllowHorizontalScroll { get; set; } = true;

        /// <summary>
        /// Czy vertical scroll jest włączony.
        /// </summary>
        public bool AllowVerticalScroll { get; set; } = true;

        /// <summary>
        /// Liczba zamarznietych kolumn.
        /// </summary>
        public int FrozenColumnCount { get; set; }

        /// <summary>
        /// Liczba zamarznietych wierszy.
        /// </summary>
        public int FrozenRowCount { get; set; }

        /// <summary>
        /// Łączna szerokość frozen data columns.
        /// </summary>
        public double FrozenDataWidth { get; set; }

        /// <summary>
        /// Łączna wysokość frozen data rows.
        /// </summary>
        public double FrozenDataHeight { get; set; }

        /// <summary>
        /// Szerokość row-header zone.
        /// </summary>
        public double RowHeaderWidth { get; set; }

        /// <summary>
        /// Wysokość column-header zone.
        /// </summary>
        public double ColumnHeaderHeight { get; set; }

        /// <summary>
        /// Wysokość wiersza filtrów renderowanego pod nagłówkami kolumn.
        /// </summary>
        public double FilterRowHeight { get; set; }

        /// <summary>
        /// Real top inset used by the data viewport inside the rendered surface host.
        /// </summary>
        public double DataTopInset { get; set; }

        /// <summary>
        /// Widoczna szerokość głównej, przewijanej strefy danych.
        /// </summary>
        public double ScrollableViewportWidth { get; set; }

        /// <summary>
        /// Widoczna wysokość głównej, przewijanej strefy danych.
        /// </summary>
        public double ScrollableViewportHeight { get; set; }

        /// <summary>
        /// Bounds strefy przecięcia frozen rows i frozen columns.
        /// </summary>
        public GridBounds FrozenCornerBounds { get; set; }

        /// <summary>
        /// Bounds strefy frozen rows przewijanej poziomo.
        /// </summary>
        public GridBounds FrozenRowsBounds { get; set; }

        /// <summary>
        /// Bounds strefy frozen columns przewijanej pionowo.
        /// </summary>
        public GridBounds FrozenColumnsBounds { get; set; }

        /// <summary>
        /// Bounds głównej strefy przewijanej w obu osiach.
        /// </summary>
        public GridBounds ScrollableBounds { get; set; }

        /// <summary>
        /// Całkowita szerokość grida (suma wszystkich kolumn).
        /// </summary>
        public double TotalWidth { get; set; }

        /// <summary>
        /// Całkowita wysokość grida (suma wszystkich wierszy).
        /// </summary>
        public double TotalHeight { get; set; }

        /// <summary>
        /// Czy grid jest w trybie edit (jedna komórka edytowana).
        /// </summary>
        public bool IsInEditMode { get; set; }

        /// <summary>
        /// Numer wersji tego stanu.
        /// </summary>
        public long Revision { get; set; }

        /// <summary>
        /// Max vertical scroll position.
        /// </summary>
        public double MaxVerticalOffset
        {
            get
            {
                var scrollableContentHeight = System.Math.Max(0, TotalHeight - DataTopInset - FrozenDataHeight);
                return System.Math.Max(0, scrollableContentHeight - ScrollableViewportHeight);
            }
        }

        /// <summary>
        /// Max horizontal scroll position.
        /// </summary>
        public double MaxHorizontalOffset
        {
            get
            {
                var scrollableContentWidth = System.Math.Max(0, TotalWidth - RowHeaderWidth - FrozenDataWidth);
                return System.Math.Max(0, scrollableContentWidth - ScrollableViewportWidth);
            }
        }

        /// <summary>
        /// Markery do narysowania przy pionowym railu przewijania.
        /// To neutralny kontrakt dla hostow platformowych.
        /// </summary>
        public IReadOnlyList<GridViewportTrackMarker> VerticalTrackMarkers { get; set; } = Array.Empty<GridViewportTrackMarker>();
    }
}
