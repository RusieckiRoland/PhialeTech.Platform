using System.Threading;
using System.Threading.Tasks;

namespace PhialeTech.Components.Shared.Services
{
    public interface IDemoRemoteGridClient
    {
        Task<DemoRemoteQueryResult> QueryAsync(DemoRemoteQueryRequest request, CancellationToken cancellationToken = default);
    }
}

