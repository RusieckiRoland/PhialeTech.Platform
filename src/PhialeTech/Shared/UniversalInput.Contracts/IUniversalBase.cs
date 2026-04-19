namespace UniversalInput.Contracts
{
    public interface IUniversalBase
    {
        /// <summary>
        /// Gets the type of the device that triggered the event, providing quick access to 
        /// information about the device responsible for initiating the action.
        /// </summary>
        DeviceType PointerDeviceType { get; }

        /// <summary>
        /// Data common for every event
        /// </summary>
        UniversalMetadata Metadata { get; set; }
    }

}
