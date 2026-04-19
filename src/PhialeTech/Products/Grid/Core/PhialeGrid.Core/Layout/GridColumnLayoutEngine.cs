using System;
using System.Collections.Generic;
using System.Linq;
using PhialeGrid.Core.Columns;

namespace PhialeGrid.Core.Layout
{
    /// <summary>
    /// Engine layoutu kolumn - oblicza pozycje i szerokości kolumn.
    /// </summary>
    public sealed class GridColumnLayoutEngine
    {
        /// <summary>
        /// Oblicza pozycje i szerokości kolumn na podstawie definicji i viewport'u.
        /// </summary>
        public IReadOnlyList<GridColumnLayout> ComputeColumnLayouts(
            IReadOnlyList<GridColumnDefinition> columns,
            double viewportWidth,
            int frozenColumnCount = 0)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));
            if (columns.Count == 0)
                return Array.Empty<GridColumnLayout>();

            var layouts = new List<GridColumnLayout>();
            double x = 0;

            // Kolumny zamarznięte mają zawsze być widoczne
            for (int i = 0; i < Math.Min(frozenColumnCount, columns.Count); i++)
            {
                var col = columns[i];
                var width = col.Width > 0 ? col.Width : col.DefaultWidth;
                layouts.Add(new GridColumnLayout
                {
                    ColumnKey = col.ColumnKey,
                    X = x,
                    Width = width,
                    IsFrozen = true,
                    DisplayOrder = i,
                });
                x += width;
            }

            // Kolumny scrollable
            for (int i = frozenColumnCount; i < columns.Count; i++)
            {
                var col = columns[i];
                var width = col.Width > 0 ? col.Width : col.DefaultWidth;
                layouts.Add(new GridColumnLayout
                {
                    ColumnKey = col.ColumnKey,
                    X = x,
                    Width = width,
                    IsFrozen = false,
                    DisplayOrder = i,
                });
                x += width;
            }

            return layouts;
        }
    }

    /// <summary>
    /// Definicja kolumny (input do layout engine).
    /// </summary>
    public sealed class GridColumnDefinition
    {
        public string ColumnKey { get; set; }
        public string Header { get; set; }
        public double Width { get; set; }
        public double DefaultWidth { get; set; } = 100;
        public double MinWidth { get; set; } = 20;
        public double MaxWidth { get; set; } = double.PositiveInfinity;
        public bool IsVisible { get; set; } = true;
        public bool IsFrozen { get; set; }
        public bool IsReadOnly { get; set; }
        public Type ValueType { get; set; } = typeof(string);
        public string ValueKind { get; set; } = "Text";
        public GridColumnEditorKind EditorKind { get; set; }
        public IReadOnlyList<string> EditorItems { get; set; } = Array.Empty<string>();
        public GridEditorItemsMode EditorItemsMode { get; set; } = GridEditorItemsMode.Suggestions;
        public string EditMask { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wynik layoutu kolumny.
    /// </summary>
    public sealed class GridColumnLayout
    {
        public string ColumnKey { get; set; }
        public double X { get; set; }
        public double Width { get; set; }
        public bool IsFrozen { get; set; }
        public int DisplayOrder { get; set; }

        public double Right => X + Width;
    }
}
