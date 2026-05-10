using System.Windows;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.Components.Wpf
{
    public partial class App : Application
    {
        private DemoApplicationServices _applicationServices;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _applicationServices = DemoApplicationServices.CreateDefault();

            var mainWindow = new MainWindow(_applicationServices)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Normal
            };

            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _applicationServices?.Dispose();
            _applicationServices = null;
            base.OnExit(e);
        }
    }
}

