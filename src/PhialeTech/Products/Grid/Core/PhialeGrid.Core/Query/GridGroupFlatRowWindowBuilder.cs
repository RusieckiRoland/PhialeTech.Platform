using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Query
{
    public static class GridGroupFlatRowWindowBuilder
    {
        public static int CountRows<T>(IReadOnlyList<GridGroupNode<T>> groups)
        {
            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            var total = 0;
            foreach (var group in groups)
            {
                total += CountRows(group);
            }

            return total;
        }

        public static GridGroupFlatRowWindow<T> BuildWindow<T>(IReadOnlyList<GridGroupNode<T>> groups, int startIndex, int length)
        {
            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var totalRowCount = CountRows(groups);
            if (totalRowCount == 0 || length == 0 || startIndex >= totalRowCount)
            {
                return new GridGroupFlatRowWindow<T>(Array.Empty<GridGroupFlatRow<T>>(), totalRowCount);
            }

            var rows = new List<GridGroupFlatRow<T>>(Math.Min(length, totalRowCount - startIndex));
            var context = new BuildContext<T>(startIndex, startIndex + length, rows);
            foreach (var group in groups)
            {
                AppendGroup(group, context);
                if (context.HasReachedEnd)
                {
                    break;
                }
            }

            return new GridGroupFlatRowWindow<T>(rows, totalRowCount);
        }

        private static int CountRows<T>(GridGroupNode<T> group)
        {
            var total = 1;
            if (!group.IsExpanded)
            {
                return total;
            }

            if (group.Children.Count > 0)
            {
                foreach (var child in group.Children)
                {
                    total += CountRows(child);
                }

                return total;
            }

            return total + group.Items.Count;
        }

        private static void AppendGroup<T>(GridGroupNode<T> group, BuildContext<T> context)
        {
            if (context.HasReachedEnd)
            {
                return;
            }

            var subtreeCount = CountRows(group);
            if (context.Cursor + subtreeCount <= context.StartIndex)
            {
                context.Cursor += subtreeCount;
                return;
            }

            if (context.ShouldCaptureCurrentRow)
            {
                context.Rows.Add(GridGroupFlatRow<T>.CreateGroupHeader(group));
            }

            context.Cursor++;
            if (!group.IsExpanded || context.HasReachedEnd)
            {
                return;
            }

            if (group.Children.Count > 0)
            {
                foreach (var child in group.Children)
                {
                    AppendGroup(child, context);
                    if (context.HasReachedEnd)
                    {
                        return;
                    }
                }

                return;
            }

            foreach (var item in group.Items)
            {
                if (context.ShouldCaptureCurrentRow)
                {
                    context.Rows.Add(GridGroupFlatRow<T>.CreateDataRow(item, group.Level + 1));
                }

                context.Cursor++;
                if (context.HasReachedEnd)
                {
                    return;
                }
            }
        }

        private sealed class BuildContext<T>
        {
            public BuildContext(int startIndex, int endExclusive, IList<GridGroupFlatRow<T>> rows)
            {
                StartIndex = startIndex;
                EndExclusive = endExclusive;
                Rows = rows;
            }

            public int StartIndex { get; }

            public int EndExclusive { get; }

            public int Cursor { get; set; }

            public IList<GridGroupFlatRow<T>> Rows { get; }

            public bool ShouldCaptureCurrentRow => Cursor >= StartIndex && Cursor < EndExclusive;

            public bool HasReachedEnd => Cursor >= EndExclusive;
        }
    }
}
