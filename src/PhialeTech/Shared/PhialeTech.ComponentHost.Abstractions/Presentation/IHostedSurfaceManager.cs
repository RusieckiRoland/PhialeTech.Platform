using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public interface IHostedSurfaceManager : IHostedSurfaceUniversalInputSink
    {
        IHostedSurfaceSessionState CurrentSession { get; }

        event EventHandler CurrentSessionChanged;

        Task<IHostedSurfaceResult> ShowAsync(IHostedSurfaceRequest request, CancellationToken cancellationToken = default);

        bool TryConfirmCurrent(string commandId = null, string payload = null);

        bool TryCancelCurrent(string commandId = null, string payload = null);

        bool TryDismissCurrent(string commandId = null, string payload = null);
    }
}
