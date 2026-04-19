using UniversalInput.Contracts;
using Avalonia.Interactivity;

namespace PhialeGis.Library.AvaloniaUi.Converters
{
    /// <summary>
    /// Avalonia counterpart of the WPF UniversalEventReverter.
    /// Avalonia doesn't expose WPF-style Manipulation* events/types,
    /// so only the 'Handled' propagation is applicable here.
    /// </summary>
    internal class UniversalEventReverter
    {
        /// <summary>
        /// Universal → Avalonia: apply Handled flag to original event args.
        /// Use in Avalonia handlers as: ApplyHandled(universal.Handled, e);
        /// </summary>
        public static void ApplyHandled(bool handled, RoutedEventArgs original)
        {
            original.Handled = handled;
        }

        // NOTE:
        // The following WPF-specific conversions have no direct Avalonia equivalents:
        // - Revert(UniversalManipulationStartingRoutedEventArgs, ManipulationStartingEventArgs, UIElement?)
        // - RevertToManipulationMode(UniversalManipulationModes)
        // - RevertToPivot(UniversalPivot)
        // Avalonia lacks ManipulationStartingEventArgs/ManipulationModes/ManipulationPivot.
        // Higher-level gesture logic should translate universal gestures to Avalonia pointers/recognizers instead.
    }
}

