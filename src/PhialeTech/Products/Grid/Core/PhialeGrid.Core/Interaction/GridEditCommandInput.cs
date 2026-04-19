using System;

namespace PhialeGrid.Core.Interaction
{
    /// <summary>
    /// Neutralne polecenie sterowania cyklem edycji aktywnej komórki.
    /// Używane przez hosty jako alternatywa dla bezpośrednich zdarzeń platformowych.
    /// </summary>
    public sealed class GridEditCommandInput : GridInputEvent
    {
        public GridEditCommandInput(
            DateTime timestamp,
            GridEditCommandKind commandKind,
            GridInputModifiers modifiers = GridInputModifiers.None)
            : base(timestamp, modifiers)
        {
            CommandKind = commandKind;
        }

        public GridEditCommandKind CommandKind { get; }
    }

    public enum GridEditCommandKind
    {
        BeginEdit,
        PostEdit,
        CancelEdit,
    }
}
