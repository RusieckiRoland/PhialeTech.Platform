// PhialeGis.Library.WinUi/Conversions/UniversalEventReverterWinUi.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using UniversalInput.Contracts;

namespace PhialeGis.Library.WinUi.Conversions
{
    /// <summary>
    /// Reverts universal event data back to WinUI-specific args where applicable.
    /// Thin mapping only; no domain logic.
    /// </summary>
    internal static class UniversalEventReverterWinUi
    {
        public static void Revert(
            UniversalManipulationStartingRoutedEventArgs universal,
            ManipulationStartingRoutedEventArgs original,
            UIElement? container = null)
        {
            original.Mode = (ManipulationModes)(uint)universal.ManipulationMode;
            if (universal.Pivot != null)
            {
                original.Pivot = new ManipulationPivot
                {
                    Center = new Windows.Foundation.Point(universal.Pivot.Center.X, universal.Pivot.Center.Y),
                    Radius = universal.Pivot.Radius
                };
            }
            if (container != null)
                original.Container = container;
        }

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
                default: break;
            }
        }
    }
}

