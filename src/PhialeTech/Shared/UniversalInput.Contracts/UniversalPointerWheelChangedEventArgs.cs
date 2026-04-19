namespace UniversalInput.Contracts
{
    /// <summary>
    /// Universal class to encapsulate pointer wheel changed event information.
    /// This class can be used to represent mouse wheel events in both WPF and UWP applications.
    /// </summary>
    public class UniversalPointerWheelChangedEventArgs : IUniversalBase
    {
        /// <summary>
        /// Gets the amount the mouse wheel has changed.
        /// </summary>
        public int Delta { get; private set; }

        /// <summary>
        /// Gets the position of the mouse pointer when the wheel event occurred.
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
        public DeviceType PointerDeviceType => DeviceType.Wheel;

        /// <summary>
        /// Constructor for UniversalPointerWheelChangedEventArgs.
        /// </summary>
        /// <param name="delta">The amount the wheel has changed.</param>
        /// <param name="position">The position of the mouse pointer.</param>
        public UniversalPointerWheelChangedEventArgs(int delta, UniversalPoint position)
        {
            Delta = delta;
            Position = position;
            Handled = false;
        }

        /// <summary>
        /// Common data applicable to each individual event.
        /// </summary>
        public UniversalMetadata Metadata { get; set; }
    }
}
