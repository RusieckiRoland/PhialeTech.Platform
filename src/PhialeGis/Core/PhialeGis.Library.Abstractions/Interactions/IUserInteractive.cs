using UniversalInput.Contracts;
using System;

namespace PhialeGis.Library.Abstractions.Interactions
{
    public interface IUserInteractive
    {
        event EventHandler<object> SurfaceShifted;

        event EventHandler<UniversalPointerRoutedEventArgs> PointerPressedUniversal;

        event EventHandler<UniversalPointerRoutedEventArgs> PointerMovedUniversal;

        event EventHandler<UniversalPointerRoutedEventArgs> PointerEnteredUniversal;

        event EventHandler<UniversalPointerRoutedEventArgs> PointerReleasedUniversal;

        event EventHandler<UniversalManipulationStartingRoutedEventArgs> ManipulationStartingUniversal;

        event EventHandler<UniversalManipulationStartedRoutedEventArgs> ManipulationStartedUniversal;

        event EventHandler<UniversalManipulationDeltaRoutedEventArgs> ManipulationDeltaUniversal;

        event EventHandler<UniversalManipulationCompletedRoutedEventArgs> ManipulationCompletedUniversal;
    }

}

