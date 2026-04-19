using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Pointer released event (mouse up, touch up).
    /// </summary>
    public sealed class GridPointerReleasedInput : GridInputEvent
    {
        public GridPointerReleasedInput(
            DateTime timestamp,
            double x,
            double y,
            GridMouseButton button = GridMouseButton.Left,
            int pointerId = -1,
            GridPointerKind pointerKind = GridPointerKind.Mouse,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            Button = button;
            PointerId = pointerId;
            PointerKind = pointerKind;
        }

        /// <summary>
        /// X pozycja wskaźnika.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y pozycja wskaźnika.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Zwolniony przycisk myszy.
        /// </summary>
        public GridMouseButton Button { get; }

        /// <summary>
        /// ID wskaźnika.
        /// </summary>
        public int PointerId { get; }

        /// <summary>
        /// Rodzaj wskaźnika.
        /// </summary>
        public GridPointerKind PointerKind { get; }
    }
}
