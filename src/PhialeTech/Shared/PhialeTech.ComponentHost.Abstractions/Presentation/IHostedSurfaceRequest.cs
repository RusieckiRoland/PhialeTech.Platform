namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public interface IHostedSurfaceRequest
    {
        HostedSurfaceKind SurfaceKind { get; }

        string ContentKey { get; }

        HostedPresentationMode PresentationMode { get; }

        HostedEntranceStyle EntranceStyle { get; }

        HostedPresentationSize Size { get; }

        HostedSheetPlacement Placement { get; }

        bool CanDismiss { get; }

        string Title { get; }

        string Payload { get; }
    }
}
