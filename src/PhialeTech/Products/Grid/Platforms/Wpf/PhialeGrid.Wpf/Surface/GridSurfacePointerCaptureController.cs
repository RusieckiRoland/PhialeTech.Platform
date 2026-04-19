using System.Windows;
using System.Windows.Input;

namespace PhialeTech.PhialeGrid.Wpf.Surface
{
    internal interface IGridSurfacePointerCaptureController
    {
        bool TryCaptureMouse(UIElement target);

        void ReleaseMouse(UIElement target);

        bool TryCaptureTouch(UIElement target, TouchDevice touchDevice);

        void ReleaseTouch(TouchDevice touchDevice);
    }

    internal sealed class GridSurfacePointerCaptureController : IGridSurfacePointerCaptureController
    {
        public static GridSurfacePointerCaptureController Default { get; } = new GridSurfacePointerCaptureController();

        public bool TryCaptureMouse(UIElement target)
        {
            return target != null && target.CaptureMouse();
        }

        public void ReleaseMouse(UIElement target)
        {
            if (target?.IsMouseCaptured == true)
            {
                target.ReleaseMouseCapture();
            }
        }

        public bool TryCaptureTouch(UIElement target, TouchDevice touchDevice)
        {
            return target != null && touchDevice != null && touchDevice.Capture(target);
        }

        public void ReleaseTouch(TouchDevice touchDevice)
        {
            touchDevice?.Capture(null);
        }
    }
}
