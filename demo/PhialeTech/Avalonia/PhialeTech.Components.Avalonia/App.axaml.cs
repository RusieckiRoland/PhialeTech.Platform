using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PhialeTech.Components.Shared.Services;

namespace PhialeTech.Components.Avalonia;

public partial class App : Application
{
    private DemoApplicationServices? _applicationServices;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _applicationServices = DemoApplicationServices.CreateDefault();
            desktop.MainWindow = new MainWindow(_applicationServices);
            desktop.Exit += (_, _) =>
            {
                _applicationServices?.Dispose();
                _applicationServices = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
