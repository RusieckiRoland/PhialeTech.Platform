using System;
using System.Collections.Generic;

namespace PhialeGrid.Core.Hierarchy
{
    public sealed class GridHierarchyPage<T>
    {
        public GridHierarchyPage(IReadOnlyList<GridHierarchyNode<T>> items, bool hasMore)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            HasMore = hasMore;
        }

        public IReadOnlyList<GridHierarchyNode<T>> Items { get; }

        public bool HasMore { get; }
    }
}
