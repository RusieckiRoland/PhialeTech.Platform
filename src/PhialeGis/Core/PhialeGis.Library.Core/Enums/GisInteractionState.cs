namespace PhialeGis.Library.Core.Enums
{
    /// <summary>
    /// Represents the different states of a GIS application.
    /// This enum is used to define the current operational context of the application,
    /// affecting how user inputs and commands are interpreted and handled.
    /// </summary>
    /// <remarks>
    /// This is a preliminary definition of application states and is likely to evolve
    /// as the application's functionality expands. Different states can be added to
    /// represent specific operational modes, such as 'Printing' or 'Data Analysis'.
    /// Additionally, this enum could be replaced or supplemented in the future with
    /// a more dynamic state management approach, such as the State design pattern,
    /// to provide a more flexible and scalable handling of application states.
    /// </remarks>
    internal enum GisInteractionState
    {
        /// <summary>
        /// The basic state of the application where user gestures and commands
        /// are interpreted as standard operations like zooming, panning, etc.
        /// </summary>
        GeneralInteraction,

        /// <summary>
        /// A state indicating that the application is in a specific command mode,
        /// such as a specialized editing or analysis mode, where user interactions
        /// are interpreted differently than in the Basic state.
        /// </summary>
        SpecializedInteraction
    }
}