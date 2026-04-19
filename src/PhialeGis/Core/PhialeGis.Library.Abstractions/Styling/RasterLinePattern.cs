using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class RasterLinePattern
    {
        public IReadOnlyList<RasterLinePatternLane> Lanes { get; set; } = Array.Empty<RasterLinePatternLane>();
    }

    public sealed class RasterLinePatternLane
    {
        public double OffsetY { get; set; }

        public int[] RunLengths { get; set; } = Array.Empty<int>();

        public bool StartsWithDash { get; set; } = true;
    }
}
