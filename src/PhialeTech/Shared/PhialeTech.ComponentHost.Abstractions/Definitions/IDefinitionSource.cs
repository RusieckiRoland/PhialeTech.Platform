namespace PhialeTech.ComponentHost.Abstractions.Definitions
{
    public interface IDefinitionSource
    {
        string SourceId { get; }

        bool TryResolve<TDefinition>(string definitionKey, out DefinitionResolution<TDefinition> resolution);
    }
}
