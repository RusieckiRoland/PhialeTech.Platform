using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalInput.Contracts
{
    public enum UniversalPointerButton
    {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 3,
    }

    /// <summary>
    /// Since inheritance in UWP event classes is restricted, this class is designed to manage 
    /// common data for all events.
    /// </summary>
    public sealed class UniversalMetadata
    {
        /// <summary>
        /// Indicates whether manipulation mode restrictions should be reset at the start 
        /// of a new interaction. Set this flag to true to clear any manipulation limitations 
        /// when beginning a new interaction.
        /// </summary>
        public bool ResetManipulationMode { get; set; }

        /// <summary>
        /// Keyboard modifiers active for the interaction.
        /// </summary>
        public UniversalModifierKeys Modifiers { get; set; }

        /// <summary>
        /// Number of clicks associated with the event when applicable.
        /// </summary>
        public int ClickCount { get; set; }

        /// <summary>
        /// Button that triggered the pointer event when applicable.
        /// </summary>
        public UniversalPointerButton ChangedButton { get; set; }
    }
}

