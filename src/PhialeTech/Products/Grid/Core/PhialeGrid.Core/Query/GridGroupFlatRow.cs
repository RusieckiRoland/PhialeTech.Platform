using System;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupFlatRow<T>
    {
        private GridGroupFlatRow(
            GridGroupFlatRowKind kind,
            int level,
            T item,
            string groupId,
            string groupColumnId,
            object groupKey,
            int groupItemCount,
            bool isExpanded)
        {
            Kind = kind;
            Level = level;
            Item = item;
            GroupId = groupId;
            GroupColumnId = groupColumnId;
            GroupKey = groupKey;
            GroupItemCount = groupItemCount;
            IsExpanded = isExpanded;
        }

        public GridGroupFlatRowKind Kind { get; }

        public int Level { get; }

        public T Item { get; }

        public string GroupId { get; }

        public string GroupColumnId { get; }

        public object GroupKey { get; }

        public int GroupItemCount { get; }

        public bool IsExpanded { get; }

        public static GridGroupFlatRow<T> CreateGroupHeader(GridGroupNode<T> group)
        {
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            return CreateGroupHeader(group.Id, group.ColumnId, group.Key, group.ItemCount, group.Level, group.IsExpanded);
        }

        public static GridGroupFlatRow<T> CreateGroupHeader(string groupId, string groupColumnId, object groupKey, int groupItemCount, int level, bool isExpanded)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group id is required.", nameof(groupId));
            }

            if (string.IsNullOrWhiteSpace(groupColumnId))
            {
                throw new ArgumentException("Group column id is required.", nameof(groupColumnId));
            }

            return new GridGroupFlatRow<T>(
                GridGroupFlatRowKind.GroupHeader,
                level,
                default(T),
                groupId,
                groupColumnId,
                groupKey,
                groupItemCount,
                isExpanded);
        }

        public static GridGroupFlatRow<T> CreateDataRow(T item, int level)
        {
            return new GridGroupFlatRow<T>(
                GridGroupFlatRowKind.DataRow,
                level,
                item,
                null,
                null,
                null,
                0,
                false);
        }
    }
}
