using System;

namespace PhialeGrid.Core.Details
{
    public sealed class GridRowDetailHeightPolicy
    {
        private GridRowDetailHeightPolicy(GridRowDetailHeightMode mode, double height)
        {
            Mode = mode;
            Height = height;
        }

        public GridRowDetailHeightMode Mode { get; }

        public double Height { get; }

        public static GridRowDetailHeightPolicy Fixed(double height)
        {
            if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Row detail fixed height must be a positive finite value.");
            }

            return new GridRowDetailHeightPolicy(GridRowDetailHeightMode.Fixed, height);
        }
    }
}
