using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Layout
{
    /// <summary>
    /// Calculator viewportu - określa co powinno być zmaterializowane na ekranie.
    /// </summary>
    public sealed class GridViewportCalculator
    {
        /// <summary>
        /// Oblicza zakres wierszy do zmaterializowania.
        /// </summary>
        public GridRealizationRange CalculateRowRange(
            double verticalOffset,
            double viewportHeight,
            IReadOnlyList<GridRowLayout> rowLayouts,
            double bufferBefore = 500,
            double bufferAfter = 500)
        {
            if (rowLayouts == null || rowLayouts.Count == 0)
                return new GridRealizationRange { Start = 0, End = 0 };

            int visibleStart = -1, visibleEnd = -1;
            int bufferedStart = -1, bufferedEnd = -1;

            double viewportTop = verticalOffset;
            double viewportBottom = verticalOffset + viewportHeight;
            double bufferedTop = verticalOffset - bufferBefore;
            double bufferedBottom = verticalOffset + viewportHeight + bufferAfter;

            for (int i = 0; i < rowLayouts.Count; i++)
            {
                var row = rowLayouts[i];
                var rowTop = row.Y;
                var rowBottom = row.Bottom;

                // Visible range
                if (rowBottom >= viewportTop && rowTop <= viewportBottom)
                {
                    if (visibleStart == -1) visibleStart = i;
                    visibleEnd = i + 1;
                }

                // Buffered range
                if (rowBottom >= bufferedTop && rowTop <= bufferedBottom)
                {
                    if (bufferedStart == -1) bufferedStart = i;
                    bufferedEnd = i + 1;
                }
            }

            return new GridRealizationRange
            {
                VisibleStart = Math.Max(0, visibleStart),
                VisibleEnd = Math.Max(0, visibleEnd),
                BufferedStart = Math.Max(0, bufferedStart),
                BufferedEnd = Math.Min(rowLayouts.Count, Math.Max(0, bufferedEnd)),
            };
        }

        /// <summary>
        /// Oblicza zakres kolumn do zmaterializowania.
        /// </summary>
        public GridRealizationRange CalculateColumnRange(
            double horizontalOffset,
            double viewportWidth,
            IReadOnlyList<GridColumnLayout> columnLayouts,
            double bufferBefore = 500,
            double bufferAfter = 500)
        {
            if (columnLayouts == null || columnLayouts.Count == 0)
                return new GridRealizationRange { Start = 0, End = 0 };

            int visibleStart = -1, visibleEnd = -1;
            int bufferedStart = -1, bufferedEnd = -1;

            double viewportLeft = horizontalOffset;
            double viewportRight = horizontalOffset + viewportWidth;
            double bufferedLeft = horizontalOffset - bufferBefore;
            double bufferedRight = horizontalOffset + viewportWidth + bufferAfter;

            for (int i = 0; i < columnLayouts.Count; i++)
            {
                var col = columnLayouts[i];
                var colLeft = col.X;
                var colRight = col.Right;

                // Visible range
                if (colRight >= viewportLeft && colLeft <= viewportRight)
                {
                    if (visibleStart == -1) visibleStart = i;
                    visibleEnd = i + 1;
                }

                // Buffered range
                if (colRight >= bufferedLeft && colLeft <= bufferedRight)
                {
                    if (bufferedStart == -1) bufferedStart = i;
                    bufferedEnd = i + 1;
                }
            }

            return new GridRealizationRange
            {
                VisibleStart = Math.Max(0, visibleStart),
                VisibleEnd = Math.Max(0, visibleEnd),
                BufferedStart = Math.Max(0, bufferedStart),
                BufferedEnd = Math.Min(columnLayouts.Count, Math.Max(0, bufferedEnd)),
            };
        }
    }

    /// <summary>
    /// Zakres realizacji (visible i buffered).
    /// </summary>
    public sealed class GridRealizationRange
    {
        public int Start { get; set; } // Deprecated, use VisibleStart
        public int End { get; set; }   // Deprecated, use VisibleEnd

        public int VisibleStart { get; set; }
        public int VisibleEnd { get; set; }
        public int BufferedStart { get; set; }
        public int BufferedEnd { get; set; }

        public int VisibleCount => Math.Max(0, VisibleEnd - VisibleStart);
        public int BufferedCount => Math.Max(0, BufferedEnd - BufferedStart);
    }

    /// <summary>
    /// Plan realizacji - opisuje co powinno być zmaterializowane.
    /// </summary>
    public sealed class GridRealizationPlan
    {
        public GridRealizationRange RowRange { get; set; }
        public GridRealizationRange ColumnRange { get; set; }
        public long Revision { get; set; }

        /// <summary>
        /// Liczba komórek do zmaterializowania (visible * visible).
        /// </summary>
        public int EstimatedCellCount => RowRange.VisibleCount * ColumnRange.VisibleCount;

        /// <summary>
        /// Liczba komórek w buforze (buffered * buffered).
        /// </summary>
        public int EstimatedBufferedCellCount => RowRange.BufferedCount * ColumnRange.BufferedCount;
    }
}
