using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class StyleAuthoringService : IStyleAuthoringService
    {
        private readonly IMutableSymbolCatalog _symbolCatalog;
        private readonly IMutableLineTypeCatalog _lineTypeCatalog;
        private readonly IMutableFillStyleCatalog _fillStyleCatalog;
        private readonly RasterLineTypeBuilder _rasterLineTypeBuilder;

        public StyleAuthoringService(
            IMutableSymbolCatalog symbolCatalog,
            IMutableLineTypeCatalog lineTypeCatalog,
            IMutableFillStyleCatalog fillStyleCatalog,
            RasterLineTypeBuilder rasterLineTypeBuilder = null)
        {
            _symbolCatalog = symbolCatalog ?? throw new ArgumentNullException(nameof(symbolCatalog));
            _lineTypeCatalog = lineTypeCatalog ?? throw new ArgumentNullException(nameof(lineTypeCatalog));
            _fillStyleCatalog = fillStyleCatalog ?? throw new ArgumentNullException(nameof(fillStyleCatalog));
            _rasterLineTypeBuilder = rasterLineTypeBuilder ?? new RasterLineTypeBuilder();
        }

        public SymbolDefinition CreateOrUpdateSymbol(SymbolDefinition definition)
        {
            StyleValidation.ValidateSymbol(definition);
            var clone = StyleDefinitionCloner.Clone(definition);
            _symbolCatalog.Set(clone);
            return StyleDefinitionCloner.Clone(clone);
        }

        public LineTypeDefinition CreateOrUpdateLineType(LineTypeDefinition definition)
        {
            StyleValidation.ValidateLineType(definition, _symbolCatalog);
            var clone = StyleDefinitionCloner.Clone(definition);
            _lineTypeCatalog.Set(clone);
            return StyleDefinitionCloner.Clone(clone);
        }

        public FillStyleDefinition CreateOrUpdateFillStyle(FillStyleDefinition definition)
        {
            StyleValidation.ValidateFillStyle(definition);
            var clone = StyleDefinitionCloner.Clone(definition);
            _fillStyleCatalog.Set(clone);
            return StyleDefinitionCloner.Clone(clone);
        }

        public LineTypeDefinition CreateRasterLineTypeFromBitmap(
            string id,
            string name,
            int width,
            int height,
            IReadOnlyList<int> pixels,
            int colorArgb,
            double strokeWidth = 1d,
            bool flow = true,
            double repeat = 0d)
        {
            var pattern = _rasterLineTypeBuilder.BuildFromArgb32(width, height, pixels);
            var lineType = new LineTypeDefinition
            {
                Id = id ?? string.Empty,
                Name = name ?? string.Empty,
                Kind = LineTypeKind.RasterPattern,
                Flow = flow,
                Repeat = repeat,
                ColorArgb = colorArgb,
                Width = strokeWidth,
                RasterPattern = pattern
            };

            return CreateOrUpdateLineType(lineType);
        }

        public LineTypeDefinition CreateVectorLineTypeFromSymbol(
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
            bool perpendicular = false)
        {
            var lineType = new LineTypeDefinition
            {
                Id = id ?? string.Empty,
                Name = name ?? string.Empty,
                Kind = LineTypeKind.VectorStamp,
                Flow = flow,
                ColorArgb = colorArgb,
                Width = strokeWidth,
                SymbolId = symbolId ?? string.Empty,
                StampSize = stampSize,
                Gap = gap,
                InitialGap = initialGap,
                OrientToTangent = orientToTangent,
                Perpendicular = perpendicular
            };

            return CreateOrUpdateLineType(lineType);
        }
    }
}
