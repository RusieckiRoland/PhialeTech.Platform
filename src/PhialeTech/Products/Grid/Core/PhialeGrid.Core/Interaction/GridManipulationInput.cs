using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Manipulation started event (multi-touch gesture start).
    /// </summary>
    public sealed class GridManipulationStartedInput : GridInputEvent
    {
        public GridManipulationStartedInput(
            DateTime timestamp,
            double x,
            double y,
            GridManipulationKind kind = GridManipulationKind.Pan,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            Kind = kind;
        }

        /// <summary>
        /// X pozycja manipulacji.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y pozycja manipulacji.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Rodzaj manipulacji (pan, pinch, rotate).
        /// </summary>
        public GridManipulationKind Kind { get; }
    }

    /// <summary>
    /// Manipulation delta event (multi-touch gesture in progress).
    /// </summary>
    public sealed class GridManipulationDeltaInput : GridInputEvent
    {
        public GridManipulationDeltaInput(
            DateTime timestamp,
            double x,
            double y,
            double deltaX = 0,
            double deltaY = 0,
            double scaleFactor = 1.0,
            double rotationDelta = 0,
            GridManipulationKind kind = GridManipulationKind.Pan,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            DeltaX = deltaX;
            DeltaY = deltaY;
            ScaleFactor = scaleFactor;
            RotationDelta = rotationDelta;
            Kind = kind;
        }

        /// <summary>
        /// X pozycja manipulacji.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y pozycja manipulacji.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Zmiana X (dla panning).
        /// </summary>
        public double DeltaX { get; }

        /// <summary>
        /// Zmiana Y (dla panning).
        /// </summary>
        public double DeltaY { get; }

        /// <summary>
        /// Współczynnik skalowania (dla pinch).
        /// > 1 = zoom in, < 1 = zoom out.
        /// </summary>
        public double ScaleFactor { get; }

        /// <summary>
        /// Delta rotacji w stopniach.
        /// </summary>
        public double RotationDelta { get; }

        /// <summary>
        /// Rodzaj manipulacji.
        /// </summary>
        public GridManipulationKind Kind { get; }
    }

    /// <summary>
    /// Manipulation completed event.
    /// </summary>
    public sealed class GridManipulationCompletedInput : GridInputEvent
    {
        public GridManipulationCompletedInput(
            DateTime timestamp,
            double x,
            double y,
            GridManipulationKind kind = GridManipulationKind.Pan,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            X = x;
            Y = y;
            Kind = kind;
        }

        /// <summary>
        /// Finalna X pozycja.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Finalna Y pozycja.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Rodzaj manipulacji.
        /// </summary>
        public GridManipulationKind Kind { get; }

        /// <summary>
        /// Czy manipulacja była successful.
        /// </summary>
        public bool IsSuccess { get; set; } = true;
    }

    /// <summary>
    /// Rodzaje multi-touch manipulacji.
    /// </summary>
    public enum GridManipulationKind
    {
        /// <summary>
        /// Przesunięcie palcami (pan/scroll).
        /// </summary>
        Pan,

        /// <summary>
        /// Zbliżenie/oddalenie (pinch zoom).
        /// </summary>
        Pinch,

        /// <summary>
        /// Rotacja dwoma palcami.
        /// </summary>
        Rotate,

        /// <summary>
        /// Inny gesture.
        /// </summary>
        Other,
    }
}
