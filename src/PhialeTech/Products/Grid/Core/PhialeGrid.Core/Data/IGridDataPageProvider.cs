using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Data
{
    public interface IGridDataPageProvider<T>
    {
        Task<GridDataPage<T>> GetPageAsync(int offset, int size, CancellationToken cancellationToken);
    }
}
