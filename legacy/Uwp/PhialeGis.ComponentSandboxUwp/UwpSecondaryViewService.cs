using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PhialeGis.ComponentSandboxUwp.Core;
using PhialeGis.ComponentSandboxUwp.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PhialeGis.ComponentSandboxUwp
{
    /// <summary>
    /// UWP implementation that opens a new CoreApplicationView and shows it as a standalone window.
    /// </summary>
    internal sealed class UwpSecondaryViewService : ISecondaryViewService
    {
        private readonly App _app;

        public UwpSecondaryViewService()
        {
            // Resolve App to access the DI service provider.
            _app = (App)Application.Current;
        }

        public async Task<int> OpenMapWindowAsync()
        {
            var currentViewId = ApplicationView.GetForCurrentView().Id;
            var newView = CoreApplication.CreateNewView();

            int newViewId = 0;

            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Resolve a fresh VM for the new window (transient). Singletons remain shared.
                var vm = _app.Services.GetRequiredService<MainPageViewModel>();

                var frame = new Frame();
                frame.Navigate(typeof(MapWindowPage), vm);

                Window.Current.Content = frame;
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });

            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                newViewId,
                ViewSizePreference.Default,
                currentViewId,
                ViewSizePreference.Default);

            return newViewId;
        }
    }
}
