using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGrid.Core.Editing
{
    public interface IGridRowValidator<in T>
    {
        Task<IReadOnlyList<GridValidationError>> ValidateAsync(T row, CancellationToken cancellationToken);
    }
}
