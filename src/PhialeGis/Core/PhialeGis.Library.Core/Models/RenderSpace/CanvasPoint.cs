using System.Runtime.InteropServices;

namespace PhialeGis.Library.Core.Models.RenderSpace
{
    /// <summary>
    /// Represents a lightweight 2D point in canvas (device) coordinate space.
    /// Equivalent in layout to SkiaSharp.SKPoint, but Skia-independent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CanvasPoint
    {
        public float X;
        public float Y;

        public CanvasPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
