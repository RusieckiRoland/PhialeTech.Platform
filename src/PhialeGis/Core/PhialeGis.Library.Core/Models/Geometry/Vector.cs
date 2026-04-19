using System;
using System.Collections.Generic;
using System.Text;

namespace PhialeGis.Library.Core.Models.Geometry
{
    public struct Vector
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}