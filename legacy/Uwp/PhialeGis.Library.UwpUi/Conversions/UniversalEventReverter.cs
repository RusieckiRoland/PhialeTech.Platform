using UniversalInput.Contracts;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using UniversalInput.Contracts.EventEnums;

namespace PhialeGis.Library.UwpUi.Conversions
{
    /// <summary>
    /// Reverts universal event data back to UWP-specific event arguments where applicable.
    /// Keep this thin: only value/type mapping, no domain logic.
    /// </summary>
    internal static class UniversalEventReverter
    {
        /// <summary>
        /// Universal → UWP (ManipulationStarting). Sets Mode and optional Pivot.
        /// </summary>
        public static void Revert(
            UniversalManipulationStartingRoutedEventArgs universal,
            ManipulationStartingRoutedEventArgs original)
        {
            original.Mode = RevertToManipulationMode(universal.ManipulationMode);
            original.Pivot = RevertToPivotOrNull(universal.Pivot);
            // Container is not set here; use the overload below if needed.
        }

        /// <summary>
        /// Universal → UWP (ManipulationStarting). Sets Mode, optional Pivot and Container.
        /// </summary>
        public static void Revert(
            UniversalManipulationStartingRoutedEventArgs universal,
            ManipulationStartingRoutedEventArgs original,
            UIElement container)
        {
            original.Mode = RevertToManipulationMode(universal.ManipulationMode);
            original.Pivot = RevertToPivotOrNull(universal.Pivot);
            if (container != null)
                original.Container = container;
        }

        /// <summary>
        /// UniversalManipulationModes → UWP ManipulationModes (bitwise-compatible).
        /// </summary>
        public static ManipulationModes RevertToManipulationMode(UniversalManipulationModes universalMode)
            => (ManipulationModes)(uint)universalMode;

        /// <summary>
        /// UniversalPivot → UWP ManipulationPivot. Returns null if universal is null.
        /// </summary>
        public static ManipulationPivot RevertToPivotOrNull(UniversalPivot universalPivot)
        {
            if (universalPivot == null) return null;
            return new ManipulationPivot
            {
                Center = ToPoint(universalPivot.Center),
                Radius = universalPivot.Radius
            };
        }

        /// <summary>
        /// Applies the universal "Handled" decision to UWP event args.
        /// Note: base Windows.UI.Xaml.RoutedEventArgs does not expose Handled; handle concrete types only.
        /// </summary>
        public static void ApplyHandled(bool handled, object original)
        {
            switch (original)
            {
                case PointerRoutedEventArgs e: e.Handled = handled; break;
                case KeyRoutedEventArgs e: e.Handled = handled; break;
                case TappedRoutedEventArgs e: e.Handled = handled; break;
                case DoubleTappedRoutedEventArgs e: e.Handled = handled; break;
                case RightTappedRoutedEventArgs e: e.Handled = handled; break;
                case HoldingRoutedEventArgs e: e.Handled = handled; break;
                case ManipulationStartingRoutedEventArgs e: e.Handled = handled; break;
                case ManipulationDeltaRoutedEventArgs e: e.Handled = handled; break;
                case ManipulationCompletedRoutedEventArgs e: e.Handled = handled; break;
                // Add more RoutedEventArgs types here if needed in the future.
                default:
                    // Base RoutedEventArgs has no Handled in UWP; nothing to do.
                    break;
            }
        }

        // ----------------- helpers -----------------

        private static Point ToPoint(UniversalPoint p)
            => new Point(p.X, p.Y);
    }
}

