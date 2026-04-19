// App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using PhialeGis.ComponentSandboxWinUi.Core;
using PhialeGis.ComponentSandboxWinUi.ViewModels;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Adapter;        // DslEditorBindings
using PhialeGis.Library.Renderer.Skia;
using PhialeGis.Library.Sync.Orchestrators; // AttachPhGis (extension)
using System;

namespace PhialeGis.ComponentSandboxWinUi
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; } = null!;
        public Window MainAppWindow { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var sc = new ServiceCollection();

            // ViewModels
            sc.AddTransient<MainPageViewModel>();

            // Core singletons
            sc.AddSingleton<PhGis>(_ => new PhGis());
            sc.AddSingleton<RenderContextResolver>();
            sc.AddSingleton<IPhRenderBackendFactory, SkiaPhRenderBackendFactory>();

            sc.AddSingleton<IDslEngine>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var resolver = sp.GetRequiredService<RenderContextResolver>();
                return new PhGisDslEngineAdapter(gis, resolver.Resolve);
            });

            sc.AddSingleton<GisInteractionManager>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var engine = sp.GetRequiredService<IDslEngine>();
                var drawingFactory = sp.GetRequiredService<IPhRenderBackendFactory>();
                var mgr = new GisInteractionManager(drawingFactory);

                // rozszerzenie z PhialeGis.Library.Sync.Orchestrators
                mgr.AttachPhGis(gis);

                // przekazujemy referencję managera do adaptera DSL (jeśli to ten typ)
                if (engine is PhGisDslEngineAdapter adapter)
                    adapter.AttachManager = a => a.AttachManager(mgr);

                // kompletne spięcie DSL (completions/exec/validate/semantics)
                DslEditorBindings.BindDsl(mgr, engine);

                return mgr;
            });

            sc.AddSingleton<IGisInteractionManager>(sp => sp.GetRequiredService<GisInteractionManager>());

            // WinUI-spec services
            sc.AddSingleton<ISecondaryViewService, WinUiSecondaryViewService>();
            sc.AddSingleton<IFilePickerService>(sp =>
            {
                var app = (App)Current;                  // okno ustawiamy dopiero w OnLaunched
                return new WinUiFilePickerService(app.MainAppWindow);
            });

            return sc.BuildServiceProvider();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Używamy MainWindow.xaml jako głównego widoku (bez Views.MainPage)
            var vm = Services.GetRequiredService<MainPageViewModel>();

            MainAppWindow = new Windows.MainWindow(vm)
            {
                Title = "PhialeGis Sandbox (WinUI 3)"
            };

            // Window nie ma DataContext – ustawiamy na korzeniu zawartości (FrameworkElement)
            if (MainAppWindow.Content is FrameworkElement root)
                root.DataContext = vm;

            MainAppWindow.Activate();
        }
    }

    // Resolver przeniesiony z UWP (namespace dostosowany)
    internal sealed class RenderContextResolver
    {
        private readonly IServiceProvider _sp;
        public RenderContextResolver(IServiceProvider sp) => _sp = sp;

        public Tuple<IViewport, IGraphicsFacade> Resolve(string _)
        {
            var mgr = _sp.GetRequiredService<GisInteractionManager>();

            if (mgr.TryResolveContext(mgr.CurrentDslTarget, out var vp, out var gfx))
                return Tuple.Create(vp, gfx);

            if (mgr.TryResolveContext(null, out vp, out gfx))
                return Tuple.Create(vp, gfx);

            throw new InvalidOperationException("No rendering viewport registered.");
        }
    }
}
