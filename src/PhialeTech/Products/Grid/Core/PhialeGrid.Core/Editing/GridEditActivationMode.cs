namespace PhialeGrid.Core.Editing
{
    /// <summary>
    /// Defines how active cell editing can be entered from user interaction.
    /// </summary>
    public enum GridEditActivationMode
    {
        /// <summary>
        /// Editing starts only through an explicit edit command.
        /// </summary>
        ExplicitCommand,

        /// <summary>
        /// Editing can start directly from user interaction such as double click,
        /// double tap or keyboard editing shortcuts.
        /// </summary>
        DirectInteraction,
    }
}
