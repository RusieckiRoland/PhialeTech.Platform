using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGis.Library.Abstractions.Ui.Rendering
{
    public interface IDevice
    {
        double GetDpiX();

        double GetDpiY();

        double CurrentWidth { get; }

        double CurrentHeight { get; }

        event EventHandler<object> ChangeVisualParams;

        event EventHandler<IDisposable> PaintSurface;
        
    }
}
