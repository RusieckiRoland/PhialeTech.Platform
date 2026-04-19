using System;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Scene;

namespace PhialeGis.Library.Core.Interactions
{
    /// <summary>
    /// Core-only hook configuration for Domain→ECS sync.
    /// Kept out of Abstractions to avoid leaking IWorld.
    /// </summary>
    public interface IRenderSyncConfig
    {
        /// <summary>Sets a global BeforeRender hook applied to all viewports/facades.</summary>
        void SetBeforeRenderHook(Action<IWorld, IViewport> hook);
    }
}
