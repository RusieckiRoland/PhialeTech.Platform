using System.Collections.Generic;
using System;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Spatial.Primitives;

namespace PhialeGis.Library.Core.Render
{
    public sealed class FillRenderRequest
    {
        public IList<PhPoint> Outer { get; set; }

        public IList<IList<PhPoint>> Holes { get; set; }

        public FillStyleDefinition FillStyle { get; set; }

        public void Validate()
        {
            if (Outer == null || Outer.Count < 3)
                throw new InvalidOperationException("Fill request must provide an outer ring.");

            if (FillStyle == null)
                throw new InvalidOperationException("Fill request must provide a fill style definition.");
        }
    }
}
