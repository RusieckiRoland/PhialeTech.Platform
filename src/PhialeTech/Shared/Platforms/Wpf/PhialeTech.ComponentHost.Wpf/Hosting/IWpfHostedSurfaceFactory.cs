using System.Windows;
using PhialeTech.ComponentHost.Abstractions.Presentation;

namespace PhialeTech.ComponentHost.Wpf.Hosting
{
    public interface IWpfHostedSurfaceFactory
    {
        bool CanCreate(IHostedSurfaceRequest request);

        FrameworkElement CreateContent(IHostedSurfaceRequest request, IHostedSurfaceManager manager);
    }
}
