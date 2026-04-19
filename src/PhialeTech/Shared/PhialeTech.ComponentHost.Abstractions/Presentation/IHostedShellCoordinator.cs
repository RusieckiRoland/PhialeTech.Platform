namespace PhialeTech.ComponentHost.Abstractions.Presentation
{
    public interface IHostedShellCoordinator
    {
        void PrepareForPresentation(IHostedSurfaceRequest request);

        void CompletePresentation(IHostedSurfaceRequest request, IHostedSurfaceResult result);
    }
}
