namespace UniversalInput.Contracts
{
    /// <summary>
    /// Universal class to encapsulate manipulation completed event information.
    /// This class is designed to mimic the behavior of ManipulationCompletedRoutedEventArgs from UWP.
    /// </summary>
    public sealed class UniversalManipulationCompletedRoutedEventArgs : IUniversalBase
    {
        /// <summary>
        /// Gets or sets a value that marks the routed event as handled.
        /// </summary>
        public bool Handled { get; set; }
        /// <summary>
        /// Gets the overall changes since the beginning of the manipulation.
        /// </summary>
        public UniversalManipulationDelta Cumulative { get; set; }
        /// <summary>
        /// Gets whether the ManipulationCompleted event occurs during inertia.
        /// </summary>
        public bool IsInertial {  get; set; }
        /// <summary>
        /// Gets the type of the device that triggered the event. This property is designed for
        /// quick access to the device information.
        /// </summary>
        public DeviceType PointerDeviceType { get; private set; }
        /// <summary>
        /// Gets the position where the manipulation originated.
        /// </summary>
        public UniversalPoint Position { get; private set; }
        /// <summary>
        /// Gets the velocities that are used for the manipulation.
        /// </summary>
        public UniversalManipulationVelocities Velocities { get; private set; }
        /// <summary>
        /// Custom metadata in response to the event.
        /// </summary>       
        public UniversalMetadata Metadata { get; set; }

        public UniversalManipulationCompletedRoutedEventArgs(bool handled, UniversalManipulationDelta cumulative, bool isInertial, DeviceType pointerDeviceType, UniversalPoint position, UniversalManipulationVelocities velocities)
        {
            Handled = handled;
            Cumulative = cumulative;
            IsInertial = isInertial;
            PointerDeviceType = pointerDeviceType;
            Position = position;
            Velocities = velocities;            
        }
    }
}
