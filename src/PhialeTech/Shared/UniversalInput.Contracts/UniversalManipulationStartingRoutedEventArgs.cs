using UniversalInput.Contracts.EventEnums;

namespace UniversalInput.Contracts
{
    public sealed class UniversalManipulationStartingRoutedEventArgs : IUniversalBase
    {
        public bool Handled { get; set; }
       
        public UniversalPivot Pivot { get; set; }

        public DeviceType PointerDeviceType { get; private set; }


        public UniversalManipulationModes ManipulationMode { get; set; }

        public UniversalManipulationStartingRoutedEventArgs(DeviceType deviceType, UniversalPivot pivot)
        {
            PointerDeviceType = deviceType;
            Handled = false;
            Pivot = pivot;            

        }

        /// <summary>
        /// Common data applicable to each individual event.
        /// </summary>
        public UniversalMetadata Metadata { get; set; }
    }
}
