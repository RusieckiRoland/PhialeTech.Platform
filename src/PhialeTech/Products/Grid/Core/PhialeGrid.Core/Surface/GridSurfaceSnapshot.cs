using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Snapshot powierzchni grida (surface snapshot).
    /// Jest to spójny opis tego, co ma być narysowane w danym momencie.
    /// Frontend otrzymuje ten snapshot i na jego podstawie renderuje interfejs.
    /// </summary>
    public sealed class GridSurfaceSnapshot
    {
        public GridSurfaceSnapshot(
            long revision,
            GridViewportState viewportState,
            IReadOnlyList<GridColumnSurfaceItem> columns,
            IReadOnlyList<GridRowSurfaceItem> rows,
            IReadOnlyList<GridCellSurfaceItem> cells,
            IReadOnlyList<GridHeaderSurfaceItem> headers = null,
            IReadOnlyList<GridOverlaySurfaceItem> overlays = null,
            IReadOnlyList<GridSelectionRegion> selectionRegions = null,
            GridCurrentCellMarker currentCell = null)
        {
            Revision = revision;
            ViewportState = viewportState ?? throw new ArgumentNullException(nameof(viewportState));
            Columns = columns ?? throw new ArgumentNullException(nameof(columns));
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Headers = headers ?? Array.Empty<GridHeaderSurfaceItem>();
            Overlays = overlays ?? Array.Empty<GridOverlaySurfaceItem>();
            SelectionRegions = selectionRegions ?? Array.Empty<GridSelectionRegion>();
            CurrentCell = currentCell;

            VisibleRowRange = ComputeVisibleRowRange();
            VisibleColumnRange = ComputeVisibleColumnRange();
        }

        /// <summary>
        /// Numer wersji tego snapshotu.
        /// Inkrementuje się za każdym razem, gdy stan grida się zmienia.
        /// Frontend może użyć tego do detektowania zmian.
        /// </summary>
        public long Revision { get; }

        /// <summary>
        /// Stan viewportu: scroll position, rozmiar, buffer settings itp.
        /// </summary>
        public GridViewportState ViewportState { get; }

        /// <summary>
        /// Kolumny obecne w tym snapshotu.
        /// </summary>
        public IReadOnlyList<GridColumnSurfaceItem> Columns { get; }

        /// <summary>
        /// Wiersze obecne w tym snapshotu.
        /// </summary>
        public IReadOnlyList<GridRowSurfaceItem> Rows { get; }

        /// <summary>
        /// Komórki obecne w tym snapshotu.
        /// Jest to podzbiór wszystkich możliwych komórek (tylko te widoczne + buffer).
        /// </summary>
        public IReadOnlyList<GridCellSurfaceItem> Cells { get; }

        /// <summary>
        /// Nagłówki (column headers, row headers, corner itp.).
        /// </summary>
        public IReadOnlyList<GridHeaderSurfaceItem> Headers { get; }

        /// <summary>
        /// Overlaye i dekoracje (selection, current cell, loading itp.).
        /// </summary>
        public IReadOnlyList<GridOverlaySurfaceItem> Overlays { get; }

        /// <summary>
        /// Regiony zaznaczenia.
        /// Frontend rysuje je na podstawie tych deskryptorów.
        /// </summary>
        public IReadOnlyList<GridSelectionRegion> SelectionRegions { get; }

        /// <summary>
        /// Marker aktualnej komórki (current cell / active cell).
        /// null = brak aktualnej komórki.
        /// </summary>
        public GridCurrentCellMarker CurrentCell { get; }

        /// <summary>
        /// Zakres widocznych wierszy (индексы w kolekcji Rows).
        /// </summary>
        public (int Start, int End) VisibleRowRange { get; }

        /// <summary>
        /// Zakres widocznych kolumn (индексy w kolekcji Columns).
        /// </summary>
        public (int Start, int End) VisibleColumnRange { get; }

        /// <summary>
        /// Czy ten snapshot jest "dirty" (wymaga pełnego rerendera).
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Dodatkowe dane dla renderers (np. theme, density itp.).
        /// </summary>
        public Dictionary<string, object> RenderingHints { get; } = new Dictionary<string, object>();

        private (int Start, int End) ComputeVisibleRowRange()
        {
            int start = 0, end = 0;
            for (int i = 0; i < Rows.Count; i++)
            {
                if (!Rows[i].IsDummy)
                {
                    if (start == 0) start = i;
                    end = i + 1;
                }
            }
            return (start, end);
        }

        private (int Start, int End) ComputeVisibleColumnRange()
        {
            int start = 0, end = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].IsVisible)
                {
                    if (start == 0) start = i;
                    end = i + 1;
                }
            }
            return (start, end);
        }
    }
}
