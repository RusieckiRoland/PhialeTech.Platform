using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Layout
{
    /// <summary>
    /// Engine layoutu wierszy - oblicza pozycje i wysokości wierszy.
    /// </summary>
    public sealed class GridRowLayoutEngine
    {
        /// <summary>
        /// Oblicza pozycje i wysokości wierszy.
        /// </summary>
        public IReadOnlyList<GridRowLayout> ComputeRowLayouts(
            IReadOnlyList<GridRowDefinition> rows,
            int frozenRowCount = 0,
            int hierarchyLevel = 0)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (rows.Count == 0)
                return Array.Empty<GridRowLayout>();

            var layouts = new List<GridRowLayout>();
            double y = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var height = row.Height > 0 ? row.Height : row.DefaultHeight;
                var indent = row.HierarchyLevel > 0 ? row.HierarchyLevel * 20 : 0;

                layouts.Add(new GridRowLayout
                {
                    RowKey = row.RowKey,
                    Y = y,
                    Height = height,
                    IsFrozen = i < frozenRowCount,
                    HierarchyIndent = indent,
                    DisplayOrder = i,
                });
                y += height;
            }

            return layouts;
        }
    }

    /// <summary>
    /// Definicja wiersza (input do layout engine).
    /// </summary>
    public sealed class GridRowDefinition
    {
        public string RowKey { get; set; }
        public double Height { get; set; }
        public double DefaultHeight { get; set; } = 30;
        public double MinHeight { get; set; } = 15;
        public double MaxHeight { get; set; } = double.PositiveInfinity;
        public bool IsVisible { get; set; } = true;
        public string HeaderText { get; set; }
        public int HierarchyLevel { get; set; }
        public bool IsHierarchyExpanded { get; set; }
        public bool HasHierarchyChildren { get; set; }
        public bool IsGroupHeader { get; set; }
        public bool IsLoadMore { get; set; }
        public bool HasDetails { get; set; }
        public bool HasDetailsExpanded { get; set; }
        public bool IsDetailsHost { get; set; }
        public object DetailsPayload { get; set; }
        public bool IsReadOnly { get; set; }
        public bool RepresentsDataRecord { get; set; } = true;
    }

    /// <summary>
    /// Wynik layoutu wiersza.
    /// </summary>
    public sealed class GridRowLayout
    {
        public string RowKey { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
        public bool IsFrozen { get; set; }
        public int HierarchyIndent { get; set; }
        public int DisplayOrder { get; set; }

        public double Bottom => Y + Height;
    }
}
