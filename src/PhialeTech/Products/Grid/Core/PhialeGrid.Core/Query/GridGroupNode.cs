using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGrid.Core.Query
{
    public sealed class GridGroupNode<T>
    {
        public GridGroupNode(string id, string columnId, object key, int level, IReadOnlyList<T> items, IReadOnlyList<GridGroupNode<T>> children, int itemCount)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Group id is required.", nameof(id));
            }

            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            Id = id;
            Key = key;
            Level = level;
            Items = items ?? Array.Empty<T>();
            Children = children ?? Array.Empty<GridGroupNode<T>>();
            ItemCount = itemCount;
            IsExpanded = true;
        }

        public string Id { get; }

        public string ColumnId { get; }

        public object Key { get; }

        public int Level { get; }

        public IReadOnlyList<T> Items { get; }

        public IReadOnlyList<GridGroupNode<T>> Children { get; }

        public int ItemCount { get; }

        public bool IsExpanded { get; set; }

        public static string BuildStableId(string parentId, string columnId, object key)
        {
            var encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Convert.ToString(key, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty));
            var prefix = string.IsNullOrEmpty(parentId) ? "root" : parentId;
            return prefix + "/" + columnId + ":" + encodedKey;
        }
    }
}
