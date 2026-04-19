namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Screen-space cursor definition (sizes in DIPs).
    /// </summary>
    public sealed class CursorSpec
    {
        public uint StrokeArgb { get; set; } = 0xFF000000u;
        public double Thickness { get; set; } = 1.5;
        public double CrosshairLength { get; set; } = 18.0;
        public double Gap { get; set; } = 6.0;
        public double ApertureSize { get; set; } = 6.0;
    }
}
