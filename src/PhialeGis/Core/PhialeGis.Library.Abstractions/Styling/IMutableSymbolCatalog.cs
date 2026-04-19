namespace PhialeGis.Library.Abstractions.Styling
{
    public interface IMutableSymbolCatalog : ISymbolCatalog
    {
        void Set(SymbolDefinition definition);
    }
}
