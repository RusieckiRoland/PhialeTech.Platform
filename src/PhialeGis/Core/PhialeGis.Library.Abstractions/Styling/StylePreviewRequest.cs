namespace PhialeGis.Library.Abstractions.Styling
{
    public sealed class StylePreviewRequest
    {
        public StylePreviewKind Kind { get; set; }

        public int WidthPx { get; set; } = 96;

        public int HeightPx { get; set; } = 32;

        public int BackgroundColorArgb { get; set; } = unchecked((int)0xFFFFFFFF);

        public LineTypeDefinition LineType { get; set; }

        public SymbolDefinition LineStampSymbol { get; set; }

        public SymbolDefinition Symbol { get; set; }

        public FillStyleDefinition FillStyle { get; set; }

        public void Validate()
        {
            switch (Kind)
            {
                case StylePreviewKind.Line:
                    if (LineType == null)
                        throw new System.InvalidOperationException("Line preview must provide a line type definition.");

                    if (LineType.Kind == LineTypeKind.VectorStamp && LineStampSymbol == null)
                    {
                        throw new System.InvalidOperationException(
                            "Vector stamp line preview must provide the resolved stamp symbol definition.");
                    }
                    break;

                case StylePreviewKind.Symbol:
                    if (Symbol == null)
                        throw new System.InvalidOperationException("Symbol preview must provide a symbol definition.");
                    break;

                case StylePreviewKind.Fill:
                    if (FillStyle == null)
                        throw new System.InvalidOperationException("Fill preview must provide a fill style definition.");
                    break;
            }
        }
    }
}
