using System;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Abstractions.Ui.Rendering
{
    public sealed class SymbolRenderRequest
    {
        public double ModelX { get; set; }

        public double ModelY { get; set; }

        public SymbolDefinition Symbol { get; set; }

        public double Size { get; set; }

        public double RotationDegrees { get; set; }

        public void Validate()
        {
            if (Symbol == null)
                throw new InvalidOperationException("Symbol render request must provide a symbol definition.");
        }
    }
}
