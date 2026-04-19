using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Core.Render
{
    public sealed class OverlayLineRenderRequest
    {
        public IList<PhPoint> Points { get; set; } = Array.Empty<PhPoint>();

        public LineTypeDefinition LineType { get; set; }

        public SymbolDefinition StampSymbol { get; set; }

        public void Validate()
        {
            if (Points == null || Points.Count < 2)
                throw new InvalidOperationException("Overlay line render request must contain at least two points.");

            if (LineType == null)
                throw new InvalidOperationException("Overlay line render request requires a line type.");

            if (LineType.Kind == LineTypeKind.VectorStamp && StampSymbol == null)
                throw new InvalidOperationException("Vector-stamp overlay line render request requires a stamp symbol.");
        }
    }
}
