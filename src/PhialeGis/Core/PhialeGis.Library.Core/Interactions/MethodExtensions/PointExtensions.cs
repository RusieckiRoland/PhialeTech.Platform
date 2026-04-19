using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Core.Models.Geometry;

namespace PhialeGis.Library.Core.Interactions.MethodExtensions
{
    internal static class PointExtensions
    {
        public static Point Delta(this Point point, CorePoint corePoint)
        {
            return new Point(corePoint.X - point.X, corePoint.Y - point.Y);
        }

        public static void Assign(this ref Point point, CorePoint corePoint)
        {
            point.X = corePoint.X;
            point.Y = corePoint.Y;
        }
    }
}
