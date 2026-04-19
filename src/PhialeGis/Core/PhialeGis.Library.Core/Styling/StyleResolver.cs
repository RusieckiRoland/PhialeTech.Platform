using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Styling;
using PhialeGis.Library.Geometry.Ecs;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class StyleResolver
    {
        private readonly ISymbolCatalog _symbolCatalog;
        private readonly ILineTypeCatalog _lineTypeCatalog;
        private readonly IFillStyleCatalog _fillStyleCatalog;

        public StyleResolver(
            ISymbolCatalog symbolCatalog,
            ILineTypeCatalog lineTypeCatalog,
            IFillStyleCatalog fillStyleCatalog)
        {
            _symbolCatalog = symbolCatalog ?? throw new ArgumentNullException(nameof(symbolCatalog));
            _lineTypeCatalog = lineTypeCatalog ?? throw new ArgumentNullException(nameof(lineTypeCatalog));
            _fillStyleCatalog = fillStyleCatalog ?? throw new ArgumentNullException(nameof(fillStyleCatalog));
        }

        public ResolvedStyleSet Resolve(PhStyleComponent style)
        {
            if (style == null)
                throw new InvalidOperationException("Renderable entity must provide a style component.");

            var lineType = ResolveLineType(style);
            return new ResolvedStyleSet
            {
                LineType = lineType,
                FillStyle = ResolveFillStyle(style),
                Symbol = ResolveSymbol(style),
                LineStampSymbol = ResolveLineStampSymbol(lineType)
            };
        }

        private LineTypeDefinition ResolveLineType(PhStyleComponent style)
        {
            if (string.IsNullOrWhiteSpace(style.LineTypeId))
                throw new InvalidOperationException("Style component must define LineTypeId.");

            if (_lineTypeCatalog.TryGet(style.LineTypeId, out var lineType))
                return lineType;

            throw new KeyNotFoundException($"Line type '{style.LineTypeId}' was not found in the catalog.");
        }

        private FillStyleDefinition ResolveFillStyle(PhStyleComponent style)
        {
            if (string.IsNullOrWhiteSpace(style.FillStyleId))
                throw new InvalidOperationException("Style component must define FillStyleId.");

            if (_fillStyleCatalog.TryGet(style.FillStyleId, out var fillStyle))
                return fillStyle;

            throw new KeyNotFoundException($"Fill style '{style.FillStyleId}' was not found in the catalog.");
        }

        private SymbolDefinition ResolveSymbol(PhStyleComponent style)
        {
            if (string.IsNullOrWhiteSpace(style.SymbolId))
                return null;

            if (_symbolCatalog.TryGet(style.SymbolId, out var symbol))
                return symbol;

            throw new KeyNotFoundException($"Symbol '{style.SymbolId}' was not found in the catalog.");
        }

        private SymbolDefinition ResolveLineStampSymbol(LineTypeDefinition lineType)
        {
            if (lineType == null || lineType.Kind != LineTypeKind.VectorStamp)
                return null;

            if (string.IsNullOrWhiteSpace(lineType.SymbolId))
                throw new InvalidOperationException("Vector stamp line type must define SymbolId.");

            if (_symbolCatalog.TryGet(lineType.SymbolId, out var symbol))
                return symbol;

            throw new KeyNotFoundException($"Vector stamp symbol '{lineType.SymbolId}' was not found in the catalog.");
        }
    }
}
