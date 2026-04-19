// Services/WinUiSecondaryViewService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using PhialeGis.ComponentSandboxWinUi.ViewModels;
using PhialeGis.ComponentSandboxWinUi.Windows;
using System.Threading.Tasks;


namespace PhialeGis.ComponentSandboxWinUi.Core
{
    internal sealed class WinUiSecondaryViewService : ISecondaryViewService
    {
        private readonly App _app; // żeby mieć dostęp do DI i głównego okna

        public WinUiSecondaryViewService()
        {
            _app = (App)Application.Current;
        }

        public Task<int> OpenMapWindowAsync()
        {
            var vm = _app.Services.GetRequiredService<MainPageViewModel>();
            var w = new Window { Title = "Map Window" };
            var page = new MapWindow { DataContext = vm };
            w.Content = page;
            w.Activate();

            return Task.FromResult(0);
        }
    }
}
