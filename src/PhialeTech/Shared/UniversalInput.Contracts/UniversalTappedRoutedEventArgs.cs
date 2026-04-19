namespace UniversalInput.Contracts
{
    /// <summary>
    /// Universal class to encapsulate tapped event information.
    /// This class is designed to mimic the behavior of TappedRoutedEventArgs from UWP.
    /// </summary>
    public class UniversalTappedRoutedEventArgs : IUniversalBase
    {
        /// <summary>
        /// Gets the location of the tap.
        /// </summary>
        public UniversalPoint Position { get; private set; }

        /// <summary>
        /// Gets or sets a value that marks the routed event as handled. A true value for Handled prevents most handlers along the event route from handling the same event again.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets the type of the device that triggered the event. This property is designed for
        /// quick access to the device information.
        /// </summary>
        public DeviceType PointerDeviceType { get; }

        /// <summary>
        /// Constructor for UniversalTappedRoutedEventArgs.
        /// </summary>
        /// <param name="position">The position where the tap occurred.</param>
        public UniversalTappedRoutedEventArgs(UniversalPoint position)
        {
            Position = position;
            Handled = false;
        }

        /// <summary>
        /// Common data applicable to each individual event.
        /// </summary>
        public UniversalMetadata Metadata { get; set; }
    }
}
