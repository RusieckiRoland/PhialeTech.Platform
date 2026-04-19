namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public sealed class HostedSurfaceResult : IHostedSurfaceResult
    {
        private string _sessionId = string.Empty;
        private string _commandId = string.Empty;
        private string _payload = string.Empty;

        public string SessionId
        {
            get => _sessionId;
            set => _sessionId = value ?? string.Empty;
        }

        public HostedSurfaceResultOutcome Outcome { get; set; }

        public string CommandId
        {
            get => _commandId;
            set => _commandId = value ?? string.Empty;
        }

        public string Payload
        {
            get => _payload;
            set => _payload = value ?? string.Empty;
        }
    }
}
