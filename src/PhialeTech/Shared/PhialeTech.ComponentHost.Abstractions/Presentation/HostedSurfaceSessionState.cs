using System;

namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public sealed class HostedSurfaceSessionState : IHostedSurfaceSessionState
    {
        private string _sessionId = string.Empty;

        public string SessionId
        {
            get => _sessionId;
            set => _sessionId = value ?? string.Empty;
        }

        public IHostedSurfaceRequest Request { get; set; }

        public DateTimeOffset OpenedAtUtc { get; set; }
    }
}
