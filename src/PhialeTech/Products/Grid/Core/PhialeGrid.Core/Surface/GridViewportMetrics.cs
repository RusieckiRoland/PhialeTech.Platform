using System.Collections.Generic;

namespace PhialeGrid.Core.Surface
{
    /// <summary>
    /// Metryki grida (rozmiary wierszy i kolumn).
    /// </summary>
    public sealed class GridViewportMetrics
    {
        public GridViewportMetrics(
            IReadOnlyList<double> rowHeights,
            IReadOnlyList<double> columnWidths)
        {
            RowHeights = rowHeights ?? throw new System.ArgumentNullException(nameof(rowHeights));
            ColumnWidths = columnWidths ?? throw new System.ArgumentNullException(nameof(columnWidths));
        }

        /// <summary>
        /// Wysokości wszystkich wierszy w pixelach.
        /// Index = row index, value = height.
        /// </summary>
        public IReadOnlyList<double> RowHeights { get; }

        /// <summary>
        /// Szerokości wszystkich kolumn w pixelach.
        /// Index = column index, value = width.
        /// </summary>
        public IReadOnlyList<double> ColumnWidths { get; }

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
        /// Łączna szerokość frozen columns.
        /// </summary>
        public double FrozenColumnWidth { get; set; }

        /// <summary>
        /// Łączna wysokość frozen rows.
        /// </summary>
        public double FrozenRowHeight { get; set; }

        /// <summary>
        /// Średnia wysokość wiersza (dla przybliżeń).
        /// </summary>
        public double AverageRowHeight { get; set; }

        /// <summary>
        /// Średnia szerokość kolumny (dla przybliżeń).
        /// </summary>
        public double AverageColumnWidth { get; set; }

        /// <summary>
        /// Numer wersji metryki.
        /// </summary>
        public long Revision { get; set; }
    }
}
