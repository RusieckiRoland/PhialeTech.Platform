using System;
using System.Runtime.InteropServices;

namespace PhialeGis.Library.Core.Models.Geometry
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PhRect
    {
        internal PhPoint Emin;
        internal PhPoint Emax;

        internal double X1 { get { return Emin.X; } }
        internal double Y1 { get { return Emin.Y; } }
        internal double X2 { get { return Emax.X; } }
        internal double Y2 { get { return Emax.Y; } }

        // Constructor to initialize the rectangle
        internal PhRect(double x1, double y1, double x2, double y2)
        {
            Emin = new PhPoint(x1, y1);
            Emax = new PhPoint(x2, y2);
        }

        internal PhRect(PhPoint emin, PhPoint emax)
        {
            Emin = new PhPoint(emin.X, emin.Y);
            Emin.Z = emin.Z;
            Emin.M = emin.M;
            Emax = new PhPoint(emax.X, emax.Y);
            Emax.Z = emax.Z;
            Emax.M = emax.M;
        }

        internal void Normalize()
        {
            if (Emin.X > Emax.X)
            {
                var tempX = Emin.X;
                Emin = new PhPoint(Emax.X, Emin.Y);
                Emax = new PhPoint(tempX, Emax.Y);
            }
            if (Emin.Y > Emax.Y)
            {
                var tempY = Emin.Y;
                Emin = new PhPoint(Emin.X, Emax.Y);
                Emax = new PhPoint(Emax.X, tempY);
            }
        }

        internal double Width => Math.Abs(X2 - X1);
        internal double Height => Math.Abs(Y2 - Y1);

        internal bool IsEmpty => Width == 0 || Height == 0;

        internal double DeltaX => Emax.X - Emin.X;
        internal double DeltaY => Emax.Y - Emin.Y;
    }
}