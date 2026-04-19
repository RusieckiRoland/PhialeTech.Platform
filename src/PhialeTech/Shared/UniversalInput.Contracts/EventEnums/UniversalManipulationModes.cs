using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalInput.Contracts.EventEnums
{
    /// <summary>
    /// Specifies the modes of manipulation available for universal interaction, 
    /// allowing combinations of various transformations and inertia effects.
    /// </summary>
    [Flags]
    public enum UniversalManipulationModes : uint
    {
        /// <summary>
        /// No manipulation interaction is allowed.
        /// </summary>
        None = 0u,

        /// <summary>
        /// Allows translation of the target along the X axis.
        /// </summary>
        TranslateX = 1u,

        /// <summary>
        /// Allows translation of the target along the Y axis.
        /// </summary>
        TranslateY = 2u,

        /// <summary>
        /// Allows translation of the target along the X axis in "rails" mode, 
        /// meaning movement is constrained or smoothed.
        /// </summary>
        TranslateRailsX = 4u,

        /// <summary>
        /// Allows translation of the target along the Y axis in "rails" mode, 
        /// meaning movement is constrained or smoothed.
        /// </summary>
        TranslateRailsY = 8u,

        /// <summary>
        /// Allows rotation of the target.
        /// </summary>
        Rotate = 0x10u,

        /// <summary>
        /// Allows scaling of the target.
        /// </summary>
        Scale = 0x20u,

        /// <summary>
        /// Applies inertia to translation actions, creating a smooth deceleration effect.
        /// </summary>
        TranslateInertia = 0x40u,

        /// <summary>
        /// Applies inertia to rotation actions, creating a smooth deceleration effect.
        /// </summary>
        RotateInertia = 0x80u,

        /// <summary>
        /// Applies inertia to scale actions, creating a smooth deceleration effect.
        /// </summary>
        ScaleInertia = 0x100u,

        /// <summary>
        /// Enables all manipulation modes except those requiring specific system support.
        /// </summary>
        All = 0xFFFFu,

        /// <summary>
        /// Enables system-driven touch interactions that require Direct Manipulation support.
        /// </summary>
        System = 0x10000u
    }
}

