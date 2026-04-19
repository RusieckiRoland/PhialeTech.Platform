using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class SymbolDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public double AnchorX { get; set; }

        public double AnchorY { get; set; }

        public double DefaultSize { get; set; } = 8d;

        public IReadOnlyList<StylePrimitive> Primitives { get; set; } = Array.Empty<StylePrimitive>();
    }
}
