using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Components
{
    /// <summary>
    /// Axis-aligned bounding box (AABB) in model space.
    /// </summary>
    public struct Bounds
    {
        public double Left;
        public double Right;
        public double Top;
        public double Bottom;

        public bool IsEmpty
        {
            get { return Right < Left || Top < Bottom; }
        }

        public static Bounds FromPoint(Vec2 p, double pad)
        {
            var b = new Bounds
            {
                Left = p.X - pad,
                Right = p.X + pad,
                Top = p.Y + pad,
                Bottom = p.Y - pad
            };
            return b;
        }

        public static Bounds FromPoints(IEnumerable<Vec2> pts)
        {
            bool any = false;
            double minX = 0, maxX = 0, minY = 0, maxY = 0;

            foreach (var p in pts)
            {
                if (!any)
                {
                    minX = maxX = p.X;
                    minY = maxY = p.Y;
                    any = true;
                }
                else
                {
                    if (p.X < minX) minX = p.X;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            if (!any) return new Bounds { Left = 1, Right = 0, Top = 0, Bottom = 1 }; // empty
            return new Bounds { Left = minX, Right = maxX, Top = maxY, Bottom = minY };
        }

        public void ExpandToInclude(Bounds other)
        {
            if (IsEmpty)
            {
                Left = other.Left; Right = other.Right;
                Top = other.Top; Bottom = other.Bottom;
                return;
            }
            if (other.IsEmpty) return;

            if (other.Left < Left) Left = other.Left;
            if (other.Right > Right) Right = other.Right;
            if (other.Bottom < Bottom) Bottom = other.Bottom;
            if (other.Top > Top) Top = other.Top;
        }

        public bool Intersects(Bounds b)
        {
            if (IsEmpty || b.IsEmpty) return false;
            return !(Right < b.Left || Left > b.Right || Top < b.Bottom || Bottom > b.Top);
        }
    }
}
