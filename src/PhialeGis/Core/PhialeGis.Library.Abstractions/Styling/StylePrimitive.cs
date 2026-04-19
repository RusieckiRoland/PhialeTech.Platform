using System;

namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class StylePrimitive
    {
        public SymbolPrimitiveKind Kind { get; set; }

        // Polyline/polygon: x1,y1,x2,y2,...
        // Circle: centerX, centerY, radius
        public double[] Coordinates { get; set; } = Array.Empty<double>();

        public int StrokeColorArgb { get; set; } = unchecked((int)0xFF163046);

        public int FillColorArgb { get; set; } = 0;

        public double StrokeWidth { get; set; } = 1d;
    }
}
