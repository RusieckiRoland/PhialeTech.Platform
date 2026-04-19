namespace UniversalInput.Contracts
{
    /// <summary>
    /// Represents a point in 2D space. This structure is designed to be compatible with Windows Runtime (WinRT) structures.
    /// In accordance with WinRT restrictions, it contains only public fields and does not have a custom constructor.
    /// This is necessary to ensure compatibility across various platforms, including UWP, where structures
    /// are limited to only containing fields and cannot have explicit constructors other than the default parameterless constructor.
    /// </summary>
    public struct UniversalPoint
    {
        /// <summary>
        /// The X-coordinate of the point.
        /// </summary>
        public double X;

        /// <summary>
        /// The Y-coordinate of the point.
        /// </summary>
        public double Y;
    }
}
