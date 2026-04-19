using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Pointer input event (mouse, touch, stylus move).
    /// </summary>
    public sealed class GridPointerMovedInput : GridInputEvent
    {
        public GridPointerMovedInput(
            DateTime timestamp,
            double x,
            double y,
            int pointerId = -1,
            GridPointerKind pointerKind = GridPointerKind.Mouse,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            PointerId = pointerId;
            PointerKind = pointerKind;
        }

        /// <summary>
        /// X pozycja wskaźnika względem viewport'u grida.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y pozycja wskaźnika względem viewport'u grida.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Unikatowy ID wskaźnika (dla multi-touch).
        /// </summary>
        public int PointerId { get; }

        /// <summary>
        /// Rodzaj wskaźnika (mouse, touch, stylus).
        /// </summary>
        public GridPointerKind PointerKind { get; }

        /// <summary>
        /// Czy to jest hover (bez naciśnięcia).
        /// </summary>
        public bool IsHover { get; set; }

        /// <summary>
        /// Czy wskaźnik jest nad gridiem.
        /// </summary>
        public bool IsOverGrid { get; set; } = true;
    }

    /// <summary>
    /// Rodzaje wskaźników.
    /// </summary>
    public enum GridPointerKind
    {
        Mouse,
        Touch,
        Stylus,
        Pen,
    }
}
