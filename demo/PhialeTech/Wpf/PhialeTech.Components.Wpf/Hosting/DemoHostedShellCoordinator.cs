using PhialeTech.ComponentHost.Abstractions.Presentation;
using PhialeTech.Components.Shared.ViewModels;

namespace PhialeTech.Components.Wpf.Hosting
{
    public sealed class DemoHostedShellCoordinator : IHostedShellCoordinator
    {
        private readonly DemoShellViewModel _shellViewModel;

        public DemoHostedShellCoordinator(DemoShellViewModel shellViewModel)
        {
            _shellViewModel = shellViewModel;
        }

        public void PrepareForPresentation(IHostedSurfaceRequest request)
        {
            if (_shellViewModel == null || request == null)
            {
                return;
            }

            if (request.PresentationMode == HostedPresentationMode.OverlaySheet)
            {
                _shellViewModel.IsDrawerOpen = false;
            }
        }

        public void CompletePresentation(IHostedSurfaceRequest request, IHostedSurfaceResult result)
        {
        }
    }
}

