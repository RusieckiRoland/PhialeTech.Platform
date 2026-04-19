using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Models.Geometry;

namespace PhialeGis.Library.Core.Interactions.Activities
{
    internal class MultiTouchActivity:DefaultActivity
    {
        private Point _basePoint = new Point(0, 0);
        public bool IsScalling { get; set; }


        public override FsmBehavior Behavior => FsmBehavior.Regular;

        protected override void OnCoreManipulationStarted(CoreManipulationInput input)
        {
            PhPoint phpoint = _viewPortManager.TranslateUPoint(StartingBasePoint);
            _viewPortManager.UpdateCentroid(phpoint);

        }

        protected override void OnCoreManipulationDelta(CoreManipulationInput input)
        {
           
            _viewPortManager.Zoom((float)input.Scale);
            var args = new RedrawEventArgs();
            _redrawRequested?.Invoke(this, args);
        }

        protected override void OnCoreManipulationCompleted(CoreManipulationInput input)
        {
            base.OnCoreManipulationCompleted(input);
        }
    }
}

