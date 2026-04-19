namespace UniversalInput.Contracts
{
    
    /// <summary>
    /// Represents the cumulative changes in a manipulation, mimicking the UWP ManipulationDelta.
    /// </summary>
    public sealed class UniversalManipulationDelta
    {
        /// <summary>
        /// Gets the cumulative translation (movement) since the beginning of the manipulation.
        /// </summary>
        public UniversalPoint Translation { get; private set; }

        /// <summary>
        /// Gets the cumulative rotation since the beginning of the manipulation, in degrees.
        /// </summary>
        public float Rotation { get; private set; }

        /// <summary>
        /// Gets the cumulative scaling factor since the beginning of the manipulation.
        /// </summary>
        public float Scale { get; private set; }

        /// <summary>
        /// Initializes a new instance of the UniversalManipulationDelta class.
        /// </summary>
        /// <param name="translation">The cumulative translation (movement).</param>
        /// <param name="rotation">The cumulative rotation, in degrees.</param>
        /// <param name="scale">The cumulative scaling factor.</param>
        public UniversalManipulationDelta(UniversalPoint translation, float rotation, float  scale)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }
    }
}

