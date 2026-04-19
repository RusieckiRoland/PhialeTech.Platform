namespace UniversalInput.Contracts.EventDetails
{
    /// <summary>
    /// Represents the velocities of a manipulation, including linear, angular, and expansion velocities.
    /// </summary>
    public struct ManipulationVelocitiesUni
    {
        /// <summary>
        /// Represents the linear velocity of the manipulation as a UniversalPoint.
        /// The X and Y components of the point indicate the velocity in respective axes.
        /// </summary>
        public UniversalPoint Linear;

        /// <summary>
        /// Represents the angular velocity of the manipulation in degrees per second.
        /// Positive values indicate clockwise rotation, while negative values indicate counter-clockwise rotation.
        /// </summary>
        public float Angular;

        /// <summary>
        /// Represents the expansion velocity of the manipulation.
        /// Positive values indicate an expanding gesture, while negative values indicate a contracting gesture.
        /// This can be used to interpret pinch or stretch interactions.
        /// </summary>
        public float Expansion;

        /// <summary>
        /// Initializes a new instance of the ManipulationVelocitiesUni structure with specified linear, angular, and expansion velocities.
        /// </summary>
        /// <param name="linear">The linear velocity of the manipulation.</param>
        /// <param name="angular">The angular velocity of the manipulation in degrees per second.</param>
        /// <param name="expansion">The expansion velocity of the manipulation.</param>
    }
}
