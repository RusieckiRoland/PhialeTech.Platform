using PhialeGis.Library.Abstractions.Ui.Rendering;
using System;

namespace PhialeGis.Library.Core.Render
{
    /// <summary>
    /// Minimal drawing context passed across render pipeline.
    /// </summary>
    public sealed class RenderContext
    {
        public RenderContext(IDisposable canvas, IViewport viewport, double dpiX, double dpiY)
        {
            Canvas = canvas;
            Viewport = viewport;
            DpiX = dpiX;
            DpiY = dpiY;
        }

        public IDisposable Canvas { get; private set; }
        public IViewport Viewport { get; private set; }
        public double DpiX { get; private set; }
        public double DpiY { get; private set; }
    }
}
