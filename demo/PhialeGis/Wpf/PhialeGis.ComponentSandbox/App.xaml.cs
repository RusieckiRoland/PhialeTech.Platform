// App.xaml.cs (WPF)
using ControlzEx.Theming;
using MahApps.Metro.Theming;
using Microsoft.Extensions.DependencyInjection;
using PhialeGis.ComponentSandbox.ViewModels;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Adapter;        // PhGisDslEngineAdapter + DslEditorBindings
using PhialeGis.Library.Renderer.Skia;
using PhialeGis.Library.Sync.Orchestrators; // AttachPhGis (extension)
using System;
using System.Windows;

namespace MahAppTestApp
{
    public partial class App : Application
    {
        // DI container compatible with Microsoft.Extensions.DependencyInjection
        public IServiceProvider Services { get; private set; } = null!;

        // Keep a reference to the main window for services that need it
        public Window MainAppWindow { get; private set; } = null!;

        public App()
        {
            // 1) MahApps theme setup (unchanged)
            var theme = ThemeManager.Current.AddLibraryTheme(
                new LibraryTheme(
                    new Uri("pack://application:,,,/PhialeGis.ComponentSandbox;component/Themes/PhialeColorsDictionary.xaml"),
                    MahAppsLibraryThemeProvider.DefaultInstance
                )
            );
            ThemeManager.Current.ChangeTheme(this, theme);

            // 2) Build service provider (mirrors WinUI pattern)
            Services = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var sc = new ServiceCollection();

            // ViewModels
            sc.AddTransient<MainWindowViewModel>();
            sc.AddTransient<MapWithDslEditorViewModel>();
            sc.AddSingleton<IPhRenderBackendFactory, SkiaPhRenderBackendFactory>();
            // Core singletons
            sc.AddSingleton<PhGis>(_ => new PhGis());
            sc.AddSingleton<RenderContextResolver>();

            // DSL engine bound to current render context resolver
            sc.AddSingleton<IDslEngine>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var resolver = sp.GetRequiredService<RenderContextResolver>();
                return new PhGisDslEngineAdapter(gis, resolver.Resolve);
            });

            // Interaction manager with full wiring (AttachPhGis + DSL bindings)
            sc.AddSingleton<GisInteractionManager>(sp =>
            {
                var gis = sp.GetRequiredService<PhGis>();
                var engine = sp.GetRequiredService<IDslEngine>();
                var drawingFactory = sp.GetRequiredService<IPhRenderBackendFactory>();
                var mgr = new GisInteractionManager(drawingFactory);

                // Extension from PhialeGis.Library.Sync.Orchestrators
                mgr.AttachPhGis(gis);

                // Pass manager reference into the DSL adapter (if applicable)
                if (engine is PhGisDslEngineAdapter adapter)
                    adapter.AttachManager = a => a.AttachManager(mgr);

                // Bind completions/exec/validate/semantics end-to-end
                DslEditorBindings.BindDsl(mgr, engine);

                return mgr;
            });

            // Expose as interface as well
            sc.AddSingleton<IGisInteractionManager>(sp => sp.GetRequiredService<GisInteractionManager>());

            // WPF-specific services (optional):
            // If you have ISecondaryViewService / IFilePickerService for WPF, register them here:
            // sc.AddSingleton<ISecondaryViewService, WpfSecondaryViewService>();
            // sc.AddSingleton<IFilePickerService>(sp =>
            // {
            //     var app = (App)Current;
            //     return new WpfFilePickerService(app.MainAppWindow);
            // });

            return sc.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Resolve VM just like in WinUI code
            var vm = Services.GetRequiredService<MainWindowViewModel>();

            // Create main window and set DataContext (WPF can set it directly on Window)
            MainAppWindow = new MainWindow
            {
                Title = "PhialeGis Sandbox (WPF)",
                DataContext = vm
            };

            MainAppWindow.Show();
        }
    }

    /// <summary>
    /// Resolves current IViewport/IGraphicsFacade pair for DSL engine.
    /// Tries current DSL target first, then falls back to default context.
    /// </summary>
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
