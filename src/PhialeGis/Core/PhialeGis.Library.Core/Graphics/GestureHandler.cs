using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.Core.Models.Geometry;

namespace PhialeGis.Library.Core.Graphics
{
    internal class GestureHandler
    {
        private Point[] _initialPoints;
        private IUserInteractive _targetControl;

        private ViewportManager _viewportManage;

        internal delegate void GestureUpdateHandler(Rect initialRect, Rect currentRect, PhMatrix transformationMatrix, ViewportManager viewportManager);

        internal event GestureUpdateHandler OnGestureUpdate;

        internal GestureHandler(IUserInteractive targetControl, ViewportManager viewportManager)
        {
            _targetControl = targetControl;
            _viewportManage = viewportManager;
        }

        internal void OnManipulationDelta(PhManipulationDeltaEventArgs e)
        {
            var points = e.Points;
            if (points.Length != 2) return;

            if (_initialPoints == null)
            {
                _initialPoints = points;
            }
            else
            {
                Rect initialRect = new Rect(_initialPoints[0], _initialPoints[1]);
                Rect currentRect = new Rect(points[0], points[1]);
                PhMatrix transformationMatrix = CalculateTransformationMatrix(initialRect, currentRect);

                OnGestureUpdate?.Invoke(initialRect, currentRect, transformationMatrix, _viewportManage);
            }
        }

        internal void OnManipulationCompleted(PhManipulationDeltaEventArgs e)
        {
            _initialPoints = null;
        }

        private PhMatrix CalculateTransformationMatrix(Rect initialRect, Rect currentRect)
        {
            double scaleX = currentRect.Width / initialRect.Width;
            double scaleY = currentRect.Height / initialRect.Height;

            double translateX = currentRect.X - initialRect.X;
            double translateY = currentRect.Y - initialRect.Y;

            return new PhMatrix(new double[,]
            {
                  { scaleX,  0,   translateX },
                  { 0,    scaleY, translateY },
                  { 0,       0,            1 }
            });
        }
    }
}