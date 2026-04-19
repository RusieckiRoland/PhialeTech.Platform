using System;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Normalized pointer input for interactive actions.
    /// </summary>
    public sealed class ActionPointerInput
    {
        /// <summary>Screen-space position (pixels, y-down).</summary>
        public UniversalPoint ScreenPosition { get; set; }

        /// <summary>Model-space position (Cartesian, y-up).</summary>
        public UniversalPoint ModelPosition { get; set; }

        /// <summary>True when ModelPosition is valid.</summary>
        public bool HasModelPosition { get; set; }

        /// <summary>Pointer device type.</summary>
        public DeviceType PointerDeviceType { get; set; }

        /// <summary>Pointer identifier (per device stream).</summary>
        public uint PointerId { get; set; }

        /// <summary>Mouse/pen button information when available.</summary>
        public PointerButton Button { get; set; } = PointerButton.None;

        /// <summary>Opaque handle indicating which draw surface should handle the input.</summary>
        public object TargetDraw { get; set; }

        /// <summary>Optional snapping result applied by the interaction manager.</summary>
        public SnapResult SnapResult { get; set; }

        /// <summary>Timestamp in UTC.</summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}

