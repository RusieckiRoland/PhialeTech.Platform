using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalInput.Contracts
{
    /// <summary>
    /// Represents the properties of a pointer point in a universal event system.
    /// This class is designed to encapsulate detailed pointer information such as the state of mouse buttons
    /// and the pressure level for touch or pen input. It provides a unified way to handle pointer
    /// input properties across different platforms (e.g., WPF and UWP), facilitating the creation of
    /// cross-platform interactive applications.
    /// </summary>
    public sealed class UniversalPointerPointProperties
    {
        /// <summary>
        /// Gets or sets a value indicating whether the left mouse button is pressed.
        /// </summary>
        public bool IsLeftButtonPressed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the right mouse button is pressed.
        /// </summary>
        public bool IsRightButtonPressed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the middle mouse button is pressed.
        /// </summary>
        public bool IsMiddleButtonPressed { get; set; }

        /// <summary>
        /// Gets or sets the pressure of the touch or pen input. This is typically a value between 0 and 1,
        /// where 0 indicates no pressure and 1 indicates maximum pressure.
        /// </summary>
        public double Pressure { get; set; }
    }
}
