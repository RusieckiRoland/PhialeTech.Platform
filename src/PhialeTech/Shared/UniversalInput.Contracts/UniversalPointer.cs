namespace UniversalInput.Contracts
{
    /// <summary>
    /// Represents pointer information for a universal event argument.
    /// </summary>
    public sealed class UniversalPointer
    {
        /// <summary>
        /// Gets the type of pointer device.
        /// </summary>
        public DeviceType PointerDeviceType { get; private set; }

        /// <summary>
        /// Gets the location of the pointer.
        /// </summary>
        public UniversalPoint Position { get; private set; }

        /// <summary>
        /// Constructor for UniversalPointer.
        /// </summary>
        /// <param name="deviceType">The type of pointer device.</param>
        /// <param name="position">The location of the pointer.</param>
        /// 

        
        public UniversalPointer(DeviceType deviceType, UniversalPoint position)
        {
            PointerDeviceType = deviceType;
            Position = position;
        }
        /// <summary>
        /// The system-generated identifier for this pointer reference.
        /// </summary>
        public uint PointerId { get; set; }

        /// <summary>
        /// Gets or sets the properties of the pointer point.
        /// This property encapsulates detailed information about the state of the pointer,
        /// such as the status of button presses (for mouse pointers), pressure information (for touch or pen),
        /// and other pointer-specific data. This allows for detailed analysis and response to the pointer
        /// input in a manner consistent with UWP's PointerPointProperties, enabling a unified approach
        /// to handling pointer input across different platforms like WPF and UWP.
        /// </summary>
        public UniversalPointerPointProperties Properties { get; set; }
    }
}
