// App.xaml.cs — AFTER (Microsoft.Extensions.DependencyInjection)
using Microsoft.Extensions.DependencyInjection;                   // NEW
using PhialeGis.ComponentSandboxUwp.Core;
using PhialeGis.ComponentSandboxUwp.ViewModels;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Adapter;
using PhialeGis.Library.Renderer.Skia;
using PhialeGis.Library.Sync.Orchestrators;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PhialeGis.ComponentSandboxUwp
{
    // Small resolver that defers manager lookup until runtime.
    // Avoids constructor-time dependency cycles without closures or Lazy<T>.
    internal sealed class RenderContextResolver
    {
        private readonly IServiceProvider _sp;
        public RenderContextResolver(IServiceProvider sp) => _sp = sp;



        public Tuple<IViewport, IGraphicsFacade> Resolve(string targetId)
        {
            var mgr = _sp.GetRequiredService<GisInteractionManager>();

            IViewport vp;
            IGraphicsFacade gfx;

            // 1) Prefer the target captured for the current DSL call
            var targetObj = mgr.CurrentDslTarget;
            if (mgr.TryResolveContext(targetObj, out vp, out gfx))
                return Tuple.Create(vp, gfx);

            // 2) Fallback: any registered viewport (first)
            if (mgr.TryResolveContext(null, out vp, out gfx))
                return Tuple.Create(vp, gfx);

            throw new InvalidOperationException("No rendering viewport registered.");
        }
    }

    public sealed partial class App : Application
    {
        private IServiceProvider _services; // replaces Unity container
        /// <summary>Global DI service provider.</summary>
        public IServiceProvider Services => _services;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            _services = ConfigureServices(); // build MS.DI service provider
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // ViewModels
            services.AddTransient<MainPageViewModel>();
            services.AddSingleton<IPhRenderBackendFactory, SkiaPhRenderBackendFactory>();
            // Core singletons
            services.AddSingleton<PhGis>(_ => new PhGis());

            // Resolver available to the engine
            services.AddSingleton<RenderContextResolver>();

            // Engine does not require the manager at construction time anymore
            services.AddSingleton<IDslEngine>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var resolver = sp.GetRequiredService<RenderContextResolver>();
                return new PhGisDslEngineAdapter(gis, resolver.Resolve);
            });

            // Manager depends on the engine (straightforward)
            services.AddSingleton<GisInteractionManager>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var engine = sp.GetRequiredService<IDslEngine>();
                var drawingFactory = sp.GetRequiredService<IPhRenderBackendFactory>();
                var mgr = new GisInteractionManager(drawingFactory);
                mgr.AttachPhGis(gis);
                ((PhGisDslEngineAdapter)engine).AttachManager = (adapter) => { adapter.AttachManager(mgr); };

                DslEditorBindings.BindDsl(mgr, engine);

                return mgr;
            });

            services.AddSingleton<IGisInteractionManager>(sp => sp.GetRequiredService<GisInteractionManager>());

            services.AddSingleton<ISecondaryViewService, UwpSecondaryViewService>();

            return services.BuildServiceProvider();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (!e.PrelaunchActivated)
            {
                if (rootFrame.Content == null)
                {
                    var vm = _services.GetRequiredService<MainPageViewModel>();
                    rootFrame.Navigate(typeof(MainPage), vm);
                }
                Window.Current.Activate();
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
