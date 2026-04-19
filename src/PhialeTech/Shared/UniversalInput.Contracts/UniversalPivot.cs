namespace UniversalInput.Contracts
{
    public sealed class UniversalPivot
    {
        //
        // Summary:
        //     Gets or sets the effective radius of rotation for rotation manipulations.
        //
        // Returns:
        //     A value in pixels.

        public double Radius {  get; set; }
        //
        // Summary:
        //     Gets or sets the center point for rotation manipulations.
        //
        // Returns:
        //     The center point for rotation manipulations.

        public UniversalPoint Center { get; set; }
    }
}

