using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Pointer cancel event raised when an active interaction ends without a normal pointer release.
    /// </summary>
    public sealed class GridPointerCanceledInput : GridInputEvent
    {
        public GridPointerCanceledInput(
            DateTime timestamp,
            double x,
            double y,
            int pointerId = -1,
            GridPointerKind pointerKind = GridPointerKind.Mouse,
            GridPointerCancelReason reason = GridPointerCancelReason.PlatformCanceled,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            PointerId = pointerId;
            PointerKind = pointerKind;
            Reason = reason;
        }

        public double X { get; }

        public double Y { get; }

        public int PointerId { get; }

        public GridPointerKind PointerKind { get; }

        public GridPointerCancelReason Reason { get; }
    }

    public enum GridPointerCancelReason
    {
        CaptureLost,
        FocusLost,
        Unloaded,
        ManipulationStarted,
        PlatformCanceled,
    }
}
