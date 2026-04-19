using System;

namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class LineTypeDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public LineTypeKind Kind { get; set; } = LineTypeKind.SimpleDash;

        public bool Flow { get; set; } = true;

        public double Repeat { get; set; }

        public int ColorArgb { get; set; } = unchecked((int)0xFF163046);

        public double Width { get; set; } = 1d;

        public StrokeLinecap Linecap { get; set; } = StrokeLinecap.Round;

        public StrokeLinejoin Linejoin { get; set; } = StrokeLinejoin.Round;

        public double MiterLimit { get; set; } = 4d;

        public double[] DashPattern { get; set; } = Array.Empty<double>();

        public double DashOffset { get; set; }

        public RasterLinePattern RasterPattern { get; set; }

        public string SymbolId { get; set; } = string.Empty;

        public double StampSize { get; set; } = 6d;

        public double Gap { get; set; } = 12d;

        public double InitialGap { get; set; }

        public bool OrientToTangent { get; set; } = true;

        public bool Perpendicular { get; set; }
    }
}
