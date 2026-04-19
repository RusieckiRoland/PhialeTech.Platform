using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalInput.Contracts
{
    public sealed class UniversalManipulationVelocities
    {
        /// <summary>
        /// The straight line velocity in device-independent pixel (DIP) per millisecond.
        /// </summary>
        public UniversalPoint Linear { get; set; }

        /// <summary>
        /// The rotational velocity in degrees per millisecond.
        /// </summary>
        public float Angular { get; set; }
        /// <summary>
        /// The expansion, or scaling, velocity in device-independent pixel (DIP) per millisecond.
        /// </summary>
        public float Expansion { get; set; }

    }
}

