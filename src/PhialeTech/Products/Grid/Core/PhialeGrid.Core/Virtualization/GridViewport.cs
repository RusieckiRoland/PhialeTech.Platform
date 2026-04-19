using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGrid.Core.Virtualization
{
    public sealed class GridViewport
    {
        public GridViewport(
            double horizontalOffset,
            double verticalOffset,
            double viewportWidth,
            double viewportHeight,
            double rowHeight,
            IReadOnlyList<double> columnWidths)
            : this(horizontalOffset, verticalOffset, viewportWidth, viewportHeight, rowHeight, null, columnWidths, 0, 1)
        {
        }

        public GridViewport(
            double horizontalOffset,
            double verticalOffset,
            double viewportWidth,
            double viewportHeight,
            IReadOnlyList<double> rowHeights,
            IReadOnlyList<double> columnWidths,
            int frozenColumnCount,
            int headerBandCount)
            : this(horizontalOffset, verticalOffset, viewportWidth, viewportHeight, 0d, rowHeights, columnWidths, frozenColumnCount, headerBandCount)
        {
        }

        private GridViewport(
            double horizontalOffset,
            double verticalOffset,
            double viewportWidth,
            double viewportHeight,
            double rowHeight,
            IReadOnlyList<double> rowHeights,
            IReadOnlyList<double> columnWidths,
            int frozenColumnCount,
            int headerBandCount)
        {
            if (horizontalOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(horizontalOffset));
            }

            if (verticalOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(verticalOffset));
            }

            if (viewportWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(viewportWidth));
            }

            if (viewportHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(viewportHeight));
            }

            if (rowHeights == null && rowHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowHeight));
            }

            if (rowHeights != null && (rowHeights.Count == 0 || rowHeights.Any(h => h <= 0)))
            {
                throw new ArgumentException("Row heights must contain at least one positive value.", nameof(rowHeights));
            }

            if (columnWidths == null)
            {
                throw new ArgumentNullException(nameof(columnWidths));
            }

            if (columnWidths.Count == 0 || columnWidths.Any(w => w <= 0))
            {
                throw new ArgumentException("Column widths must contain at least one positive value.", nameof(columnWidths));
            }

            if (frozenColumnCount < 0 || frozenColumnCount > columnWidths.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(frozenColumnCount));
            }

            if (headerBandCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(headerBandCount));
            }

            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            RowHeight = rowHeights == null ? rowHeight : 0d;
            VariableRowHeights = rowHeights;
            ColumnWidths = columnWidths;
            FrozenColumnCount = frozenColumnCount;
            HeaderBandCount = headerBandCount;
        }

        public double HorizontalOffset { get; }

        public double VerticalOffset { get; }

        public double ViewportWidth { get; }

        public double ViewportHeight { get; }

        public double RowHeight { get; }

        public IReadOnlyList<double> VariableRowHeights { get; }

        public IReadOnlyList<double> ColumnWidths { get; }

        public int FrozenColumnCount { get; }

        public int HeaderBandCount { get; }

        public bool HasVariableRowHeights => VariableRowHeights != null;

        public GridRange CalculateVisibleRows(int totalRows, int overscan = 2)
        {
            if (totalRows < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalRows));
            }

            if (overscan < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(overscan));
            }

            if (totalRows == 0)
            {
                return new GridRange(0, 0);
            }

            if (!HasVariableRowHeights)
            {
                var start = (int)Math.Floor(VerticalOffset / RowHeight);
                var visible = (int)Math.Ceiling(ViewportHeight / RowHeight);
                return ClampRange(start, visible, totalRows, overscan);
            }

            var rowHeights = VariableRowHeights;
            var maxRows = Math.Min(totalRows, rowHeights.Count);
            var startIndex = FindRowIndex(rowHeights, VerticalOffset, maxRows);
            var endIndex = FindRowIndex(rowHeights, VerticalOffset + ViewportHeight, maxRows);
            var visibleLength = Math.Max(1, endIndex - startIndex + 1);
            return ClampRange(startIndex, visibleLength, maxRows, overscan);
        }

        public GridRange CalculateFrozenColumns()
        {
            return new GridRange(0, FrozenColumnCount);
        }

        public GridRange CalculateVisibleColumns(int overscan = 1)
        {
            return CalculateScrollableColumns(overscan);
        }

        public GridRange CalculateScrollableColumns(int overscan = 1)
        {
            if (overscan < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(overscan));
            }

            var frozenWidth = ColumnWidths.Take(FrozenColumnCount).Sum();
            var availableWidth = Math.Max(0d, ViewportWidth - frozenWidth);
            if (availableWidth <= 0d)
            {
                return new GridRange(FrozenColumnCount, 0);
            }

            var startIndex = FrozenColumnCount;
            var x = 0d;
            while (startIndex < ColumnWidths.Count && x + ColumnWidths[startIndex] <= HorizontalOffset)
            {
                x += ColumnWidths[startIndex];
                startIndex++;
            }

            if (startIndex >= ColumnWidths.Count)
            {
                return new GridRange(ColumnWidths.Count, 0);
            }

            var endIndex = startIndex;
            var widthAccumulated = x;
            while (endIndex < ColumnWidths.Count && widthAccumulated < HorizontalOffset + availableWidth)
            {
                widthAccumulated += ColumnWidths[endIndex];
                endIndex++;
            }

            var length = Math.Max(1, endIndex - startIndex);
            return ClampRange(startIndex, length, ColumnWidths.Count, overscan);
        }

        public GridRange CalculateHeaderBands()
        {
            return new GridRange(0, HeaderBandCount);
        }

        private static int FindRowIndex(IReadOnlyList<double> heights, double offset, int maxRows)
        {
            var currentOffset = 0d;
            for (var i = 0; i < maxRows; i++)
            {
                var next = currentOffset + heights[i];
                if (offset < next)
                {
                    return i;
                }

                currentOffset = next;
            }

            return Math.Max(0, maxRows - 1);
        }

        private static GridRange ClampRange(int start, int length, int total, int overscan)
        {
            var expandedStart = Math.Max(0, start - overscan);
            var expandedEnd = Math.Min(total, start + length + overscan);
            return new GridRange(expandedStart, Math.Max(0, expandedEnd - expandedStart));
        }
    }
}
