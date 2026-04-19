using System;

namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public interface IHostedSurfaceSessionState
    {
        string SessionId { get; }

        IHostedSurfaceRequest Request { get; }

        DateTimeOffset OpenedAtUtc { get; }
    }
}
