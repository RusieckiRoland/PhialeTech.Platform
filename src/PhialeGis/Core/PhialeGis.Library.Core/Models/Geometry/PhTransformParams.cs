using System.Runtime.InteropServices;

namespace PhialeGis.Library.Core.Models.Geometry
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PhTransformParams
    {
        internal PhRect ActiveWindow;

        internal PhPoint Centroid;
        internal double Scale;

        internal PhTransformParams(PhRect activeWindow, PhPoint centroid, double scale)
        {
            ActiveWindow = activeWindow;
            Centroid = centroid;
            Scale = scale;
        }
    }
}