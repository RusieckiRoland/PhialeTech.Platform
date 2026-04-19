using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Hierarchy
{
    public interface IGridHierarchyPagingProvider<T> : IGridHierarchyProvider<T>
    {
        Task<GridHierarchyPage<T>> LoadChildrenPageAsync(GridHierarchyNode<T> parent, int offset, int size, CancellationToken cancellationToken);
    }
}
