using System;
using System.Windows.Controls.Primitives;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    internal readonly struct GridPopupLayoutMetrics : IEquatable<GridPopupLayoutMetrics>
    {
        public GridPopupLayoutMetrics(PlacementMode placement, double maxHeight)
        {
            Placement = placement;
            MaxHeight = maxHeight;
        }

        public PlacementMode Placement { get; }

        public double MaxHeight { get; }

        public bool Equals(GridPopupLayoutMetrics other)
        {
            return Placement == other.Placement && MaxHeight.Equals(other.MaxHeight);
        }

        public override bool Equals(object obj)
        {
            return obj is GridPopupLayoutMetrics other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Placement * 397) ^ MaxHeight.GetHashCode();
            }
        }
    }

    internal static class GridPopupLayoutConfiguration
    {
        private const double ScreenMargin = 16d;
        private const double PreferredBelowHeight = 280d;

        public static GridPopupLayoutMetrics ResolveContextMenuLayout(double availableAbove, double availableBelow)
        {
            var usableAbove = Math.Max(0d, availableAbove - ScreenMargin);
            var usableBelow = Math.Max(0d, availableBelow - ScreenMargin);

            var openBelow = usableBelow >= PreferredBelowHeight || usableBelow >= usableAbove;
            var maxHeight = openBelow ? usableBelow : usableAbove;

            if (maxHeight <= 0d)
            {
                maxHeight = Math.Max(usableAbove, usableBelow);
            }

            return new GridPopupLayoutMetrics(
                openBelow ? PlacementMode.Bottom : PlacementMode.Top,
                maxHeight);
        }
    }
}
