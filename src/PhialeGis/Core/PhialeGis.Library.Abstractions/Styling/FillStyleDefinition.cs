using System;

namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class FillStyleDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public FillStyleKind Kind { get; set; } = FillStyleKind.Solid;

        public int ForeColorArgb { get; set; } = unchecked((int)0xFFFFFFFF);

        public int BackColorArgb { get; set; } = 0;

        public GradientDirection GradientDirection { get; set; } = GradientDirection.LeftToRight;

        public FillDirection FillDirection { get; set; } = FillDirection.Diagonal45;

        public double FillFactor { get; set; } = 1d;

        public int TileWidth { get; set; } = 8;

        public int TileHeight { get; set; } = 8;

        public byte[] TileBytes { get; set; } = Array.Empty<byte>();

        public double HatchSpacing { get; set; } = 8d;

        public double HatchThickness { get; set; } = 1d;
    }
}
