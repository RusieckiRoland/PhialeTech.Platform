using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Hierarchy
{
    public sealed class GridHierarchyNode<T>
    {
        public GridHierarchyNode(string id, T item, bool canExpand, string parentId = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            ParentId = parentId;
            PathId = string.IsNullOrEmpty(parentId) ? id : parentId + "/" + id;
            Item = item;
            CanExpand = canExpand;
            Children = Array.Empty<GridHierarchyNode<T>>();
        }

        public string Id { get; }

        public string ParentId { get; }

        public string PathId { get; }

        public T Item { get; }

        public bool CanExpand { get; }

        public bool IsExpanded { get; set; }

        public bool IsChildrenLoaded { get; set; }

        public bool HasMoreChildren { get; set; }

        public int LoadedChildrenCount { get; set; }

        public IReadOnlyList<GridHierarchyNode<T>> Children { get; set; }
    }
}
