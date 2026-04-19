using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Query
{
    public interface IGridGroupedQueryDataProvider<T>
    {
        Task<GridGroupedQueryResult<T>> QueryGroupedAsync(GridGroupedQueryRequest request, CancellationToken cancellationToken);
    }
}
