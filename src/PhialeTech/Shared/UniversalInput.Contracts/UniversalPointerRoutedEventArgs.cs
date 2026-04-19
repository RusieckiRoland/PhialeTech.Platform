namespace UniversalInput.Contracts
{
    /// <summary>
    /// Universal class to encapsulate pointer event information.
    /// This class is designed to mimic the behavior of PointerRoutedEventArgs from UWP.
    /// </summary>
    public sealed class UniversalPointerRoutedEventArgs : IUniversalBase
    {
        /// <summary>
        /// Gets the pointer associated with the event.
        /// </summary>
        public UniversalPointer Pointer { get; private set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the pointer event was handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Device type that triggered the event
        /// </summary>
        public DeviceType PointerDeviceType => Pointer.PointerDeviceType;

        /// <summary>
        /// Constructor for UniversalPointerRoutedEventArgs.
        /// </summary>
        /// <param name="pointer">The UniversalPointer associated with the event.</param>
        public UniversalPointerRoutedEventArgs(UniversalPointer pointer)
        {
            Pointer = pointer;
            Handled = false;
        }

               

        /// <summary>
        /// Common data applicable to each individual event.
        /// </summary>
        public UniversalMetadata Metadata { get; set; }
    }
}
