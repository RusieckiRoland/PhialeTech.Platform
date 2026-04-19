using System.Collections.Generic;
using System;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Core.Render
{
    public sealed class LineRenderRequest
    {
        public IList<PhPoint> Points { get; set; }

        public LineTypeDefinition LineType { get; set; }

        public SymbolDefinition StampSymbol { get; set; }

        public void Validate()
        {
            if (Points == null || Points.Count < 2)
                throw new InvalidOperationException("Styled line request must provide at least two points.");

            if (LineType == null)
                throw new InvalidOperationException("Styled line request must provide a line type definition.");

            if (LineType.Kind == LineTypeKind.VectorStamp && StampSymbol == null)
            {
                throw new InvalidOperationException(
                    "Vector stamp line request must provide the resolved stamp symbol definition.");
            }
        }
    }
}
