using PhialeTech.ComponentHost.Abstractions.Presentation;

namespace PhialeTech.ComponentHost.Presentation
{
    public sealed class NullHostedShellCoordinator : IHostedShellCoordinator
    {
        public void PrepareForPresentation(IHostedSurfaceRequest request)
        {
        }

        public void CompletePresentation(IHostedSurfaceRequest request, IHostedSurfaceResult result)
        {
        }
    }
}
