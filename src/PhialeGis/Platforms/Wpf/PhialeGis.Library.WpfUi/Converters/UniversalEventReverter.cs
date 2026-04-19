using UniversalInput.Contracts;
using UniversalInput.Contracts.EventEnums;
using System.Windows;
using System.Windows.Input;

namespace PhialeGis.Library.WpfUi.Converters
{
    internal class UniversalEventReverter
    {
        /// <summary>
        /// Konwersja: Universal → WPF (ManipulationStarting).
        /// Ustawia Mode, opcjonalnie Pivot i ManipulationContainer.
        /// </summary>
        public static void Revert(
            UniversalManipulationStartingRoutedEventArgs universal,
            ManipulationStartingEventArgs original,
            UIElement? container = null)
        {
            original.Mode = RevertToManipulationMode(universal.ManipulationMode);

            if (universal.Pivot != null)
                original.Pivot = RevertToPivot(universal.Pivot);

            if (container != null)
                original.ManipulationContainer = container;
        }

        /// <summary>
        /// Mapowanie UniversalManipulationModes → WPF ManipulationModes.
        /// </summary>
        public static ManipulationModes RevertToManipulationMode(UniversalManipulationModes universalMode)
        {
            return (ManipulationModes)(uint)universalMode;
        }

        /// <summary>
        /// Mapowanie UniversalPivot → WPF ManipulationPivot.
        /// </summary>
        public static ManipulationPivot RevertToPivot(UniversalPivot universalPivot)
        {
            return new ManipulationPivot
            {
                Center = new System.Windows.Point(universalPivot.Center.X, universalPivot.Center.Y),
                Radius = universalPivot.Radius
            };
        }

        /// <summary>
        /// Uniwersalne ustawienie Handled jeśli chcesz „odbić” decyzję z warstwy universal.
        /// Użyj w handlerach WPF: ApplyHandled(universal.Handled, e);
        /// </summary>
        public static void ApplyHandled(bool handled, RoutedEventArgs original)
        {
            original.Handled = handled;
        }
    }
}

