using System;
using System.IO;
using PhialeTech.ComponentHost.Definitions;
using PhialeTech.ComponentHost.State;

namespace PhialeTech.Components.Shared.Services
{
    public sealed class DemoApplicationServices : IDisposable
    {
        private DemoApplicationServices(ApplicationStateManager applicationStateManager, DefinitionManager definitionManager)
        {
            ApplicationStateManager = applicationStateManager ?? throw new ArgumentNullException(nameof(applicationStateManager));
            DefinitionManager = definitionManager ?? throw new ArgumentNullException(nameof(definitionManager));
        }

        public ApplicationStateManager ApplicationStateManager { get; }

        public DefinitionManager DefinitionManager { get; }

        public static DemoApplicationServices CreateDefault(string applicationName = "PhialeTech.Components", string rootDirectory = null)
        {
            var store = new JsonApplicationStateStore(applicationName, rootDirectory);
            return new DemoApplicationServices(new ApplicationStateManager(store), DemoDefinitionCatalog.CreateManager());
        }

        public static DemoApplicationServices CreateIsolatedForWindow()
        {
            var rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "PhialeTech.Components",
                "ui-state",
                Guid.NewGuid().ToString("N"));
            return CreateDefault("PhialeTech.Components", rootDirectory);
        }

        public void Dispose()
        {
            ApplicationStateManager.Dispose();
        }
    }
}
