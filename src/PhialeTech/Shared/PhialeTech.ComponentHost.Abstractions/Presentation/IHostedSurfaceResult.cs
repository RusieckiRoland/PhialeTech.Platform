namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public interface IHostedSurfaceResult
    {
        string SessionId { get; }

        HostedSurfaceResultOutcome Outcome { get; }

        string CommandId { get; }

        string Payload { get; }
    }
}
