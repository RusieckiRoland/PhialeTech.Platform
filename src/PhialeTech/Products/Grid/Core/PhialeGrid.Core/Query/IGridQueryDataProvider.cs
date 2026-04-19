using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Query
{
    public interface IGridQueryDataProvider<T>
    {
        Task<GridQueryResult<T>> QueryAsync(GridQueryRequest request, CancellationToken cancellationToken);
    }
}
