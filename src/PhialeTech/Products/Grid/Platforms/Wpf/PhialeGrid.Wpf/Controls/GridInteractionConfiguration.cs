using System;
using System.Windows;

namespace PhialeTech.PhialeGrid.Wpf.Controls
{
    public enum GridInteractionMode
    {
        Classic,
        Touch,
        Auto,
    }

    public enum GridDensity
    {
        Compact,
        Comfortable,
        Touch,
    }

    internal enum GridInputOrigin
    {
        Mouse,
        Keyboard,
        Touch,
        Stylus,
    }

    internal readonly struct GridDensityMetrics : IEquatable<GridDensityMetrics>
    {
        public GridDensityMetrics(
            GridDensity density,
            double rowHeight,
            double detailRowHeight,
            double rowHeaderWidth,
            Thickness cellPadding,
            Thickness headerPadding,
            Thickness filterTextPadding,
            double filterClearButtonSize,
            Thickness rowDetailsMargin,
            double headerMenuButtonSize)
        {
            Density = density;
            RowHeight = rowHeight;
            DetailRowHeight = detailRowHeight;
            RowHeaderWidth = rowHeaderWidth;
            CellPadding = cellPadding;
            HeaderPadding = headerPadding;
            FilterTextPadding = filterTextPadding;
            FilterClearButtonSize = filterClearButtonSize;
            RowDetailsMargin = rowDetailsMargin;
            HeaderMenuButtonSize = headerMenuButtonSize;
        }

        public GridDensity Density { get; }

        public double RowHeight { get; }

        public double DetailRowHeight { get; }

        public double RowHeaderWidth { get; }

        public Thickness CellPadding { get; }

        public Thickness HeaderPadding { get; }

        public Thickness FilterTextPadding { get; }

        public double FilterClearButtonSize { get; }

        public Thickness RowDetailsMargin { get; }

        public double HeaderMenuButtonSize { get; }

        public bool Equals(GridDensityMetrics other)
        {
            return Density == other.Density &&
                   RowHeight.Equals(other.RowHeight) &&
                   DetailRowHeight.Equals(other.DetailRowHeight) &&
                   RowHeaderWidth.Equals(other.RowHeaderWidth) &&
                   CellPadding.Equals(other.CellPadding) &&
                   HeaderPadding.Equals(other.HeaderPadding) &&
                   FilterTextPadding.Equals(other.FilterTextPadding) &&
                   FilterClearButtonSize.Equals(other.FilterClearButtonSize) &&
                   RowDetailsMargin.Equals(other.RowDetailsMargin) &&
                   HeaderMenuButtonSize.Equals(other.HeaderMenuButtonSize);
        }

        public override bool Equals(object obj)
        {
            return obj is GridDensityMetrics other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Density;
                hash = (hash * 397) ^ RowHeight.GetHashCode();
                hash = (hash * 397) ^ DetailRowHeight.GetHashCode();
                hash = (hash * 397) ^ RowHeaderWidth.GetHashCode();
                hash = (hash * 397) ^ CellPadding.GetHashCode();
                hash = (hash * 397) ^ HeaderPadding.GetHashCode();
                hash = (hash * 397) ^ FilterTextPadding.GetHashCode();
                hash = (hash * 397) ^ FilterClearButtonSize.GetHashCode();
                hash = (hash * 397) ^ RowDetailsMargin.GetHashCode();
                hash = (hash * 397) ^ HeaderMenuButtonSize.GetHashCode();
                return hash;
            }
        }
    }

    internal static class GridInteractionConfiguration
    {
        public static GridInteractionMode ResolveInteractionMode(GridInteractionMode requestedMode, GridInteractionMode autoMode)
        {
            return requestedMode == GridInteractionMode.Auto
                ? autoMode
                : requestedMode;
        }

        public static GridInteractionMode UpdateAutoInteractionMode(GridInteractionMode currentAutoMode, GridInputOrigin inputOrigin)
        {
            return inputOrigin == GridInputOrigin.Touch || inputOrigin == GridInputOrigin.Stylus
                ? GridInteractionMode.Touch
                : GridInteractionMode.Classic;
        }

        public static GridDensityMetrics ResolveDensityMetrics(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return new GridDensityMetrics(
                        GridDensity.Comfortable,
                        38d,
                        34d,
                        32d,
                        new Thickness(10d, 6d, 10d, 6d),
                        new Thickness(12d, 10d, 12d, 10d),
                        new Thickness(10d, 8d, 28d, 8d),
                        20d,
                        new Thickness(32d, 0d, 0d, 8d),
                        22d);
                case GridDensity.Touch:
                    return new GridDensityMetrics(
                        GridDensity.Touch,
                        46d,
                        42d,
                        40d,
                        new Thickness(12d, 10d, 12d, 10d),
                        new Thickness(14d, 12d, 14d, 12d),
                        new Thickness(12d, 10d, 32d, 10d),
                        24d,
                        new Thickness(40d, 0d, 0d, 8d),
                        30d);
                default:
                    return new GridDensityMetrics(
                        GridDensity.Compact,
                        30d,
                        28d,
                        28d,
                        new Thickness(8d, 4d, 8d, 4d),
                        new Thickness(10d, 8d, 10d, 8d),
                        new Thickness(8d, 6d, 24d, 6d),
                        18d,
                        new Thickness(28d, 0d, 0d, 8d),
                        18d);
            }
        }

        public static double ResolveRowIndicatorWidth(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 20d;
                case GridDensity.Touch:
                    return 24d;
                default:
                    return 20d;
            }
        }

        public static double ResolveSelectionCheckboxWidth(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 20d;
                case GridDensity.Touch:
                    return 22d;
                default:
                    return 18d;
            }
        }

        public static double ResolveRowMarkerWidth(GridDensity density, bool showSelectionCheckbox, bool showNumber, int numberDigits = 1)
        {
            if (!showSelectionCheckbox && !showNumber)
            {
                return 0d;
            }

            var checkboxWidth = ResolveSelectionCheckboxWidth(density);
            var digitWidth = ResolveRowNumberDigitWidth(density);
            var textPadding = ResolveRowNumberPadding(density);
            var gap = showSelectionCheckbox && showNumber
                ? ResolveMarkerGap(density)
                : 0d;
            var numberWidth = showNumber
                ? Math.Max(ResolveMinimumNumberWidth(density), (digitWidth * Math.Max(1, numberDigits)) + textPadding)
                : 0d;

            if (showSelectionCheckbox && showNumber)
            {
                return checkboxWidth + gap + numberWidth;
            }

            if (showNumber)
            {
                return numberWidth;
            }

            return checkboxWidth;
        }

        private static double ResolveRowNumberDigitWidth(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 8d;
                case GridDensity.Touch:
                    return 9d;
                default:
                    return 7d;
            }
        }

        private static double ResolveRowNumberPadding(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 10d;
                case GridDensity.Touch:
                    return 12d;
                default:
                    return 8d;
            }
        }

        private static double ResolveMinimumNumberWidth(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 28d;
                case GridDensity.Touch:
                    return 34d;
                default:
                    return 24d;
            }
        }

        private static double ResolveMarkerGap(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 6d;
                case GridDensity.Touch:
                    return 8d;
                default:
                    return 4d;
            }
        }

        public static double ResolveRowActionWidth(GridDensity density)
        {
            switch (density)
            {
                case GridDensity.Comfortable:
                    return 18d;
                case GridDensity.Touch:
                    return 20d;
                default:
                    return 16d;
            }
        }
    }
}
