using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhialeGis.Library.Core.Models.Geometry
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("X = {X}, Y = {Y}, Z = {Z}, M = {M}")]
    public struct PhPoint
    {
        internal double X { get; set; }
        internal double Y { get; set; }
        internal double? Z { get; set; } // Nullable for 3D support
        internal double? M { get; set; } // Nullable for measure support

        // Constructor for 2D points
        public PhPoint(double x, double y)
        {
            X = x;
            Y = y;
            Z = null;
            M = null;
        }

        // Constructor for 3D points
        public PhPoint(double x, double y, double z)
            : this(x, y)
        {
            Z = z;
        }

        // Constructor for 3D points with measure
        public PhPoint(double x, double y, double z, double m)
            : this(x, y, z)
        {
            M = m;
        }
    }
}