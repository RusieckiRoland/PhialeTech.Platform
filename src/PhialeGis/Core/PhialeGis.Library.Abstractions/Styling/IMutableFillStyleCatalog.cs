namespace PhialeGis.Library.Abstractions.Styling
{
    public interface IMutableFillStyleCatalog : IFillStyleCatalog
    {
        void Set(FillStyleDefinition definition);
    }
}
