using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Enums;
using System;

namespace PhialeGis.Library.Core.Interactions
{
    internal class DeviceDecorator : IDevice
    {
        private const double DefaultSize = 100;
        internal double HorizontalScreenResolutionDpi { get; private set; }
        internal double VerticalScreenResolutionDpi { get; private set; }
        internal int HorizontalPrintResolutionDpi { get; private set; }
        internal int VerticalPrintResolutionDpi { get; private set; }

        public double CurrentWidth => _phControl != null ? _phControl.CurrentWidth : DefaultSize;

        public double CurrentHeight => _phControl != null ? _phControl.CurrentHeight : DefaultSize;

        public object CompositionAdapter { get; }

        public event EventHandler<object> ChangeVisualParams;

        public event EventHandler<IDisposable> PaintSurface;

        private IDevice _phControl;

        private ActiveDevice _deviceType = ActiveDevice.Screen;

        internal DeviceDecorator(IDevice phControl)
        {
            InitializeDpi();
            _phControl = phControl;
            if (_phControl != null)
            {
                _phControl.ChangeVisualParams += OnVisualParamsChanged1;
                _phControl.PaintSurface += OnPaintSurface;
            }
        }

        internal DeviceDecorator()
        {
        }

        #region Event mirroring

        private void OnVisualParamsChanged1(object sender, object e)
        {
            var handler = ChangeVisualParams;
            handler?.Invoke(sender, e);
        }

        private void OnPaintSurface(object sender, IDisposable e)
        {
            var handler = PaintSurface;
            handler?.Invoke(sender, e);
        }

        #endregion Event mirroring

        public double GetDpiX()
        {
            return _deviceType == ActiveDevice.Screen ? HorizontalScreenResolutionDpi : HorizontalPrintResolutionDpi;
        }

        public double GetDpiY()
        {
            return _deviceType == ActiveDevice.Screen ? VerticalScreenResolutionDpi : VerticalPrintResolutionDpi;
        }

        internal void InitializeDpi()
        {
            (HorizontalScreenResolutionDpi, VerticalScreenResolutionDpi) = GetScreenDpi();
            (HorizontalPrintResolutionDpi, VerticalPrintResolutionDpi) = GetPrinterDpi();
        }

        internal (int dpiX, int dpiY) GetPrinterDpi()
        {
            return (300, 300);
        }

        internal (double dpiX, double dpiY) GetScreenDpi()
        {
            if (_phControl == null)
            {
                return (96, 96);
            }
            return (_phControl.GetDpiX(), _phControl.GetDpiY());
        }
    }
}
