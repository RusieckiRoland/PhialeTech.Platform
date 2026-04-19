using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Core.Graphics;
using System;

namespace PhialeGis.Library.Core.Interactions
{
    internal class ManipulationHandler : RedrawTriggerBase
    {
        private ViewportManager _viewportManager;

        public ManipulationHandler(ViewportManager viewportManager,
            IUserInteractive userInteractive, EventHandler<RedrawEventArgs> redrawRequested) : base(redrawRequested)
        {
            _viewportManager = viewportManager;
            userInteractive.SurfaceShifted += OnSurfaceShifted;
        }

        private void OnSurfaceShifted(object sender, object e)
        {
            if (e is SurfaceMovement surfaceMovement)
            {
                _viewportManager.ScrollByScreenOffset(surfaceMovement.XMovement, surfaceMovement.YMovement);
            }

            TriggerRedraw();
        }
    }
}