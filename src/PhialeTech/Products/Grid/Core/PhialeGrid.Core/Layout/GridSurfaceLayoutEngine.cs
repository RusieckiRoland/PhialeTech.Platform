using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Surface;

namespace PhialeGrid.Core.Layout
{
    /// <summary>
    /// Główny engine layoutu powierzchni - łączy layout kolumn i wierszy.
    /// </summary>
    public sealed class GridSurfaceLayoutEngine
    {
        private readonly GridColumnLayoutEngine _columnEngine = new GridColumnLayoutEngine();
        private readonly GridRowLayoutEngine _rowEngine = new GridRowLayoutEngine();

        /// <summary>
        /// Oblicza kompletny layout surface'u.
        /// </summary>
        public GridSurfaceLayout ComputeLayout(
            IReadOnlyList<GridColumnDefinition> columns,
            IReadOnlyList<GridRowDefinition> rows,
            int frozenColumnCount = 0,
            int frozenRowCount = 0)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var columnLayouts = _columnEngine.ComputeColumnLayouts(columns, 0, frozenColumnCount);
            var rowLayouts = _rowEngine.ComputeRowLayouts(rows, frozenRowCount);

            var layout = new GridSurfaceLayout
            {
                ColumnLayouts = columnLayouts,
                RowLayouts = rowLayouts,
            };

            // Obliczam bounds dla każdej komórki  
            foreach (var rowLayout in rowLayouts)
            {
                foreach (var colLayout in columnLayouts)
                {
                    var bounds = new GridBounds(colLayout.X, rowLayout.Y, colLayout.Width, rowLayout.Height);
                    layout.CellBounds[($"{rowLayout.RowKey}_{colLayout.ColumnKey}")] = bounds;
                }
            }

            // Obliczam total size
            layout.TotalWidth = columnLayouts.Count > 0 ? columnLayouts[columnLayouts.Count - 1].Right : 0;
            layout.TotalHeight = rowLayouts.Count > 0 ? rowLayouts[rowLayouts.Count - 1].Bottom : 0;

            return layout;
        }
    }

    /// <summary>
    /// Wynik layoutu surface'u.
    /// </summary>
    public sealed class GridSurfaceLayout
    {
        public IReadOnlyList<GridColumnLayout> ColumnLayouts { get; set; } = Array.Empty<GridColumnLayout>();
        public IReadOnlyList<GridRowLayout> RowLayouts { get; set; } = Array.Empty<GridRowLayout>();
        public Dictionary<string, GridBounds> CellBounds { get; } = new Dictionary<string, GridBounds>();
        public double TotalWidth { get; set; }
        public double TotalHeight { get; set; }
    }
}
