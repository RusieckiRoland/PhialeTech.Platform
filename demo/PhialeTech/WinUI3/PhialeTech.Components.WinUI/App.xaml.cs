using Microsoft.UI.Xaml;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;

namespace PhialeTech.Components.WinUI
{
    public partial class App : Application
    {
        private DemoApplicationServices _applicationServices;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _applicationServices = DemoApplicationServices.CreateDefault();

            var window = new MainWindow(_applicationServices)
            {
                Title = "PhialeTech StoryBook"
            };
            window.Closed += HandleMainWindowClosed;

            window.Activate();
        }

        private void HandleMainWindowClosed(object sender, WindowEventArgs args)
        {
            _applicationServices?.Dispose();
            _applicationServices = null;
        }
    }
}

