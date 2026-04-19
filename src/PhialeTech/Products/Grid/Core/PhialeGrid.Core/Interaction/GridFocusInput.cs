using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Focus input event (grid gained/lost focus).
    /// </summary>
    public sealed class GridFocusInput : GridInputEvent
    {
        public GridFocusInput(
            DateTime timestamp,
            bool hasFocus,
            GridFocusCause cause = GridFocusCause.Programmatic)
            : base(timestamp)
        {
            HasFocus = hasFocus;
            Cause = cause;
        }

        /// <summary>
        /// Czy grid uzyskał fokus (true) czy stracił (false).
        /// </summary>
        public bool HasFocus { get; }

        /// <summary>
        /// Przyczyna zmiany fokusa.
        /// </summary>
        public GridFocusCause Cause { get; }
    }

    /// <summary>
    /// Przyczyna zmiany fokusa.
    /// </summary>
    public enum GridFocusCause
    {
        /// <summary>
        /// Zmiana z kodu.
        /// </summary>
        Programmatic,

        /// <summary>
        /// Klik myszy.
        /// </summary>
        Mouse,

        /// <summary>
        /// Tab/Shift+Tab.
        /// </summary>
        KeyboardNavigation,

        /// <summary>
        /// Touch.
        /// </summary>
        Touch,

        /// <summary>
        /// Inne.
        /// </summary>
        Other,
    }
}
