using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Hierarchy
{
    public interface IGridHierarchyProvider<T>
    {
        Task<IReadOnlyList<GridHierarchyNode<T>>> LoadChildrenAsync(GridHierarchyNode<T> parent, CancellationToken cancellationToken);
    }
}
