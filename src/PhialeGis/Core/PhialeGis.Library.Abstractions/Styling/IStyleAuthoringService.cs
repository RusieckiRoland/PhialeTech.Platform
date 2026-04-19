using System.Collections.Generic;

namespace PhialeGis.Library.Abstractions.Styling
{
    public interface IStyleAuthoringService
    {
        SymbolDefinition CreateOrUpdateSymbol(SymbolDefinition definition);

        LineTypeDefinition CreateOrUpdateLineType(LineTypeDefinition definition);

        FillStyleDefinition CreateOrUpdateFillStyle(FillStyleDefinition definition);

        LineTypeDefinition CreateRasterLineTypeFromBitmap(
            string id,
            string name,
            int width,
            int height,
            IReadOnlyList<int> pixels,
            int colorArgb,
            double strokeWidth = 1d,
            bool flow = true,
            double repeat = 0d);

        LineTypeDefinition CreateVectorLineTypeFromSymbol(
            string id,
            string name,
            string symbolId,
            int colorArgb,
            double strokeWidth,
            double stampSize,
            double gap,
            double initialGap = 0d,
            bool flow = true,
            bool orientToTangent = true,
            bool perpendicular = false);
    }
}
