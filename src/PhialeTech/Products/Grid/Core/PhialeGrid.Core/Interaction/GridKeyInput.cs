using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Keyboard input event.
    /// </summary>
    public sealed class GridKeyInput : GridInputEvent
    {
        public GridKeyInput(
            DateTime timestamp,
            GridKey key,
            GridKeyEventKind kind = GridKeyEventKind.KeyDown,
            GridInputModifiers modifiers = GridInputModifiers.None,
            char? character = null)
            : base(timestamp, modifiers)
        {
            Key = key;
            Kind = kind;
            Character = character;
        }

        /// <summary>
        /// Wciśnięty klawisz.
        /// </summary>
        public GridKey Key { get; }

        /// <summary>
        /// Rodzaj zdarzenia klawiatury.
        /// </summary>
        public GridKeyEventKind Kind { get; }

        /// <summary>
        /// Znakcharacter, jeśli to zdarzenie text input (dla KeyDown z printable character).
        /// </summary>
        public char? Character { get; }

        /// <summary>
        /// Czy to jest powtórzenie (key repeat).
        /// </summary>
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Czy ten input jest obsłużony (handled).
        /// </summary>
        public bool IsHandled { get; set; }
    }

    /// <summary>
    /// Rodzaj zdarzenia klawiatury.
    /// </summary>
    public enum GridKeyEventKind
    {
        KeyDown,
        KeyUp,
    }

    /// <summary>
    /// Kody klawiszy.
    /// </summary>
    public enum GridKey
    {
        Unknown,
        Cancel,
        Back,
        Tab,
        LineFeed,
        Clear,
        Return,
        Pause,
        CapsLock,
        Escape,
        Space,
        PageUp,
        PageDown,
        End,
        Home,
        Left,
        Up,
        Right,
        Down,
        Select,
        Print,
        Execute,
        PrintScreen,
        Insert,
        Delete,
        Help,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
        Multiply,
        Add,
        Separator,
        Subtract,
        Decimal,
        Divide,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        NumLock,
        Scroll,
        LeftShift,
        RightShift,
        LeftControl,
        RightControl,
        LeftAlt,
        RightAlt,
        LeftSuper,
        RightSuper,
        ContextMenu,
    }
}
