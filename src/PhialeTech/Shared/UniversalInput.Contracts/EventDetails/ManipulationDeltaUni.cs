namespace UniversalInput.Contracts.EventDetails
{
    /// <summary>
    /// Represents the changes that occurred during a manipulation.
    /// This includes translation, rotation, scale, and expansion information.
    /// </summary>
    public sealed class ManipulationDeltaUni
    {
        /// <summary>
        /// Gets the expansion vector, indicating the change in distance between two points.
        /// This is useful for pinch or stretch interactions.
        /// </summary>
        public UniversalVector Expansion { get; private set; }

        /// <summary>
        /// Gets the rotation angle in degrees, indicating the change in orientation.
        /// Positive values indicate clockwise rotation, negative values indicate counter-clockwise rotation.
        /// </summary>
        public double Rotation { get; private set; }

        /// <summary>
        /// Gets the scale vector, indicating the change in scaling.
        /// X and Y components represent the scale change in respective dimensions.
        /// </summary>
        public UniversalVector Scale { get; private set; }

        /// <summary>
        /// Gets the translation vector, indicating the change in position.
        /// X and Y components represent the translation change in respective axes.
        /// </summary>
        public UniversalVector Translation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ManipulationDeltaUni class with specified translation,
        /// rotation, scale, and expansion vectors.
        /// </summary>
        /// <param name="translation">The translation vector representing the change in position.</param>
        /// <param name="rotation">The rotation angle in degrees representing the change in orientation.</param>
        /// <param name="scale">The scale vector representing the change in scaling.</param>
        /// <param name="expansion">The expansion vector representing the change in distance between points.</param>
        public ManipulationDeltaUni(UniversalVector translation, double rotation, UniversalVector scale, UniversalVector expansion)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
            Expansion = expansion;
        }
    }
}
