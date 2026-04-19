using PhialeGis.Library.Core.Models.Geometry;

namespace PhialeGis.Library.Core.Interactions
{
    public class PhManipulationDeltaEventArgs
    {
        internal Point[] Points { get; }

        internal PhManipulationDeltaEventArgs(Point[] points)
        {
            Points = points;
        }
    }
}