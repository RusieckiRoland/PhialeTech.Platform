using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Wheel/scroll input event.
    /// </summary>
    public sealed class GridWheelInput : GridInputEvent
    {
        public GridWheelInput(
            DateTime timestamp,
            double x,
            double y,
            double deltaX = 0,
            double deltaY = 0,
            GridWheelMode mode = GridWheelMode.Pixel,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            DeltaX = deltaX;
            DeltaY = deltaY;
            Mode = mode;
        }

        /// <summary>
        /// X pozycja kółka myszy.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y pozycja kółka myszy.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Zmiana w osi X (horizontal scroll).
        /// </summary>
        public double DeltaX { get; }

        /// <summary>
        /// Zmiana w osi Y (vertical scroll).
        /// </summary>
        public double DeltaY { get; }

        /// <summary>
        /// Jednostka scrollowania (pixel, line, page).
        /// </summary>
        public GridWheelMode Mode { get; }
    }

    /// <summary>
    /// Tryby scrollowania kolarem.
    /// </summary>
    public enum GridWheelMode
    {
        /// <summary>
        /// Pixele.
        /// </summary>
        Pixel,

        /// <summary>
        /// Linie (wysokość wiersza).
        /// </summary>
        Line,

        /// <summary>
        /// Strony (wysokość viewport'u).
        /// </summary>
        Page,
    }
}
