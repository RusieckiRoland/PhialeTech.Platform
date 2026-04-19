using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Pointer pressed event (mouse down, touch down).
    /// </summary>
    public sealed class GridPointerPressedInput : GridInputEvent
    {
        public GridPointerPressedInput(
            DateTime timestamp,
            double x,
            double y,
            GridMouseButton button = GridMouseButton.Left,
            int clickCount = 1,
            int pointerId = -1,
            GridPointerKind pointerKind = GridPointerKind.Mouse,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            Button = button;
            ClickCount = clickCount;
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
        /// Wciśnięty przycisk myszy.
        /// </summary>
        public GridMouseButton Button { get; }

        /// <summary>
        /// Liczba kliknięć (1 = jedno, 2 = double click).
        /// </summary>
        public int ClickCount { get; }

        /// <summary>
        /// ID wskaźnika.
        /// </summary>
        public int PointerId { get; }

        /// <summary>
        /// Rodzaj wskaźnika.
        /// </summary>
        public GridPointerKind PointerKind { get; }

        /// <summary>
        /// Czy przycisk jest utrzymywany (drag).
        /// </summary>
        public bool IsPressed { get; set; } = true;
    }

    /// <summary>
    /// Przyciski myszy.
    /// </summary>
    public enum GridMouseButton
    {
        Left,
        Middle,
        Right,
    }
}
