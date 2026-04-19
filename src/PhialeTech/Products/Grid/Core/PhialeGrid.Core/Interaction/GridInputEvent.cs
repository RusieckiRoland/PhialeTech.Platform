using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Bazowa klasa dla wszystkich input event'ów grida.
    /// Frontend konwertuje platformowe event'y na te ujednolicone input event'y.
    /// </summary>
    public abstract class GridInputEvent
    {
        protected GridInputEvent(DateTime timestamp, GridInputModifiers modifiers = GridInputModifiers.None)
        {
            Timestamp = timestamp;
            Modifiers = modifiers;
        }

        /// <summary>
        /// Czas zdarzenia.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Klawisze modyfikujące podczas zdarzenia (Shift, Ctrl, Alt).
        /// </summary>
        public GridInputModifiers Modifiers { get; }

        /// <summary>
        /// Czy Shift był wciśnięty.
        /// </summary>
        public bool HasShift => (Modifiers & GridInputModifiers.Shift) != 0;

        /// <summary>
        /// Czy Ctrl/Cmd był wciśnięty.
        /// </summary>
        public bool HasControl => (Modifiers & GridInputModifiers.Control) != 0;

        /// <summary>
        /// Czy Alt był wciśnięty.
        /// </summary>
        public bool HasAlt => (Modifiers & GridInputModifiers.Alt) != 0;

        /// <summary>
        /// Czy Super/Windows/Command był wciśnięty.
        /// </summary>
        public bool HasSuper => (Modifiers & GridInputModifiers.Super) != 0;
    }

    /// <summary>
    /// Klawisze modyfikujące.
    /// </summary>
    [Flags]
    public enum GridInputModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4,
        Super = 8,
    }
}
