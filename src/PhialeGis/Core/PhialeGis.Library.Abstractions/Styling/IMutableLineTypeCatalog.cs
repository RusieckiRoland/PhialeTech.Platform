namespace PhialeGis.Library.Abstractions.Styling
{
    public interface IMutableLineTypeCatalog : ILineTypeCatalog
    {
        void Set(LineTypeDefinition definition);
    }
}
