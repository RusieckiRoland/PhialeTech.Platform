namespace UniversalInput.Contracts
{


    /// <summary>
    /// Universal class to encapsulate manipulation started event information.
    /// This class is designed to mimic the behavior of ManipulationStartedRoutedEventArgs from UWP.
    /// </summary>
    public sealed class UniversalManipulationStartedRoutedEventArgs : IUniversalBase
    {
        
        
        /// <summary>
        /// Gets the position where the manipulation originated.
        /// </summary>
        public UniversalPoint Position { get; private set; }

        /// <summary>
        /// Gets or sets a value that marks the routed event as handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the type of the device that triggered the event. This property is designed for
        /// quick access to the device information.
        /// </summary>
        public DeviceType PointerDeviceType { get; private set; }

        /// <summary>
        /// Gets the cumulative data of the manipulation (e.g., cumulative translation, rotation, scale).
        /// This mimics the Cumulative property from UWP's ManipulationStartedRoutedEventArgs.
        /// </summary>
        public UniversalManipulationDelta  Cumulative { get; private set; }

        /// <summary>
        /// Constructor for UniversalManipulationStartedRoutedEventArgs.
        /// </summary>
        /// <param name="position">The position where the manipulation started.</param>
        /// <param name="device">The device that initiated the manipulation.</param>
        /// <param name="cumulative">The cumulative manipulation data (translation, rotation, scale).</param>
        public UniversalManipulationStartedRoutedEventArgs(UniversalPoint position, DeviceType device, UniversalManipulationDelta cumulative)
        {
            Position = position;
            PointerDeviceType = device;
            Cumulative = cumulative;
            Handled = false;
        }

        /// <summary>
        /// Common data applicable to each individual event.
        /// </summary>
        public UniversalMetadata Metadata { get; set; }

    }
}
