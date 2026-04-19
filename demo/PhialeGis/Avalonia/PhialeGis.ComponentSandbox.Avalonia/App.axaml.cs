using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PhialeGis.ComponentSandbox.Avalonia.ViewModels;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Adapter;
using PhialeGis.Library.Renderer.Skia;
using PhialeGis.Library.Sync.Orchestrators;

namespace PhialeGis.ComponentSandbox.Avalonia
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; } = null!;
        public Window? MainAppWindow { get; private set; }

        public override void Initialize()
        {
            StartupTrace.Log("App.Initialize: begin");

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                StartupTrace.Log($"UnhandledException: {e.ExceptionObject}");
            TaskScheduler.UnobservedTaskException += (_, e) =>
                StartupTrace.Log($"UnobservedTaskException: {e.Exception}");

            AvaloniaXamlLoader.Load(this);
            StartupTrace.Log("App.Initialize: XAML loaded");

            Services = ConfigureServices();
            StartupTrace.Log("App.Initialize: services configured");
        }

        private IServiceProvider ConfigureServices()
        {
            var sc = new ServiceCollection();

            sc.AddSingleton<PhGis>();
            sc.AddSingleton<IPhRenderBackendFactory, SkiaPhRenderBackendFactory>();
            sc.AddSingleton<RenderContextResolver>();

            sc.AddSingleton<IDslEngine>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var resolver = sp.GetRequiredService<RenderContextResolver>();
                return new PhGisDslEngineAdapter(gis, resolver.Resolve);
            });

            sc.AddTransient<MainWindowViewModel>();

            sc.AddSingleton<GisInteractionManager>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var engine = sp.GetRequiredService<IDslEngine>();
                var drawingFactory = sp.GetRequiredService<IPhRenderBackendFactory>();

                var mgr = new GisInteractionManager(drawingFactory);
                mgr.AttachPhGis(gis);

                if (engine is PhGisDslEngineAdapter adapter)
                    adapter.AttachManager = a => a.AttachManager(mgr);

                DslEditorBindings.BindDsl(mgr, engine);
                return mgr;
            });

            sc.AddSingleton<IGisInteractionManager>(sp => sp.GetRequiredService<GisInteractionManager>());

            return sc.BuildServiceProvider();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            StartupTrace.Log($"OnFrameworkInitializationCompleted: lifetime={ApplicationLifetime?.GetType().FullName ?? "<null>"}");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += (_, _) => StartupTrace.Log("Desktop: Exit");
                desktop.ShutdownRequested += (_, e) => StartupTrace.Log($"Desktop: ShutdownRequested cancel={e.Cancel}");

                var vm = Services.GetRequiredService<MainWindowViewModel>();
                MainAppWindow = new MainWindow
                {
                    Title = "PhialeGis Sandbox (Avalonia)",
                    DataContext = vm
                };

                MainAppWindow.Opened += (_, _) => StartupTrace.Log("MainWindow: Opened");
                MainAppWindow.Closing += (_, e) => StartupTrace.Log($"MainWindow: Closing cancel={e.Cancel}");
                MainAppWindow.Closed += (_, _) => StartupTrace.Log("MainWindow: Closed");

                desktop.MainWindow = MainAppWindow;
                StartupTrace.Log("OnFrameworkInitializationCompleted: desktop.MainWindow assigned");

                // VS+WSL can sporadically miss implicit show; enforce initial window activation.
                MainAppWindow.Show();
                MainAppWindow.Activate();
                StartupTrace.Log("OnFrameworkInitializationCompleted: MainWindow.Show+Activate");
            }

            base.OnFrameworkInitializationCompleted();
            StartupTrace.Log("OnFrameworkInitializationCompleted: base completed");
        }
    }

    internal sealed class RenderContextResolver
    {
        private readonly IServiceProvider _sp;

        public RenderContextResolver(IServiceProvider sp) => _sp = sp;

        public Tuple<IViewport, IGraphicsFacade> Resolve(string _)
        {
            var mgr = _sp.GetRequiredService<GisInteractionManager>();
            _sp.GetRequiredService<IPhRenderBackendFactory>();

            if (mgr.TryResolveContext(mgr.CurrentDslTarget, out var vp, out var gfx))
                return Tuple.Create(vp, gfx);

            if (mgr.TryResolveContext(null, out vp, out gfx))
                return Tuple.Create(vp, gfx);

            throw new InvalidOperationException("No rendering viewport registered.");
        }
    }
}
