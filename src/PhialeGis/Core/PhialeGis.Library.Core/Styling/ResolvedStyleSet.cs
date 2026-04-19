using PhialeGis.Library.Abstractions.Styling;

namespace PhialeGis.Library.Core.Styling
{
    public sealed class ResolvedStyleSet
    {
        public LineTypeDefinition LineType { get; set; }

        public FillStyleDefinition FillStyle { get; set; }

        public SymbolDefinition Symbol { get; set; }

        public SymbolDefinition LineStampSymbol { get; set; }
    }
}
