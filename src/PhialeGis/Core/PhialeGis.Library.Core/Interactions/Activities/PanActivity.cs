using PhialeGis.Library.Abstractions.Ui.Enums;
using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Interactions.MethodExtensions;
using PhialeGis.Library.Core.Models.Geometry;

namespace PhialeGis.Library.Core.Interactions.Activities
{
    internal class PanActivity : DefaultActivity
    {
        private Point _basePoint = new Point(0, 0);

        public override FsmBehavior Behavior => FsmBehavior.Regular;
        bool pointerPressed = false;
        uint? PointerId { get; set; } = null;

        protected override void OnCorePointerPressed(CorePointerInput input)
        {
            if (PointerId != null)
            {
                return;
            }

            PointerId = input.PointerId;
            if (pointerPressed) return; 
            pointerPressed = true;
            if (input.DeviceType == CoreDeviceType.Pen || input.DeviceType == CoreDeviceType.Mouse || input.DeviceType == CoreDeviceType.Touch)
            {
                _basePoint = new Point(input.Position.X, input.Position.Y);
            }
            else
                base.OnCorePointerEntered(input);
        }

        protected override void OnCorePointerMoved(CorePointerInput input)
        {           
            if (input.PointerId != PointerId) return;
            if (input.DeviceType == CoreDeviceType.Pen || input.DeviceType == CoreDeviceType.Mouse || input.DeviceType == CoreDeviceType.Touch)
            {
                MoveSurface(input);
            }
            else
                base.OnCorePointerMoved(input);
        }

        private void MoveSurface(CorePointerInput input)
        {
            
            var shifVector = _basePoint.Delta(input.Position);
            _basePoint.X = input.Position.X;
            _basePoint.Y = input.Position.Y;
            _viewPortManager.PanByScreenOffset(shifVector.X, shifVector.Y);
            var args = new RedrawEventArgs();
            _redrawRequested?.Invoke(this, args);
            if (input.DeviceType == CoreDeviceType.Mouse)
            { CursorManager.SetCursor(CursorType.Hand); }
        }

        protected override void OnCorePointerReleased(CorePointerInput input)
        {
            CursorManager.SetCursor(CursorType.Arrow);
            FinishActivityAction();
        }

        
    }
}
