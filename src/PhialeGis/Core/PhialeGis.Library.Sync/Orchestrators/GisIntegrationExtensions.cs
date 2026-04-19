// PhialeGis.Library.Sync/Orchestrators/GisIntegrationExtensions.cs
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Scene;
using PhialeGis.Library.Domain.Map;


namespace PhialeGis.Library.Sync.Orchestrators
{
    /// <summary>
    /// Registers SimplePhGisWorldSync in the Core render loop. No device/backend dependencies here.
    /// </summary>
    public static class GisIntegrationExtensions
    {
        public static void AttachPhGis(this IGisInteractionManager manager, PhGis gis)
        {
            var cfg = manager as IRenderSyncConfig;
            if (cfg == null || gis == null) return;

            var sync = new SimplePhGisWorldSync();

            manager.SetActionResultCommitter(new LineStringActionResultCommitter(gis));
            manager.SetSnapService(new PhGisSnapService(gis));
            

            cfg.SetBeforeRenderHook(delegate (IWorld world, IViewport viewport)
            {
                sync.Sync(gis, world, viewport);
                // Actual drawing is performed by Core render pipeline on the control's SKCanvas.
            });
        }
    }
}
