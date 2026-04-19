namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDocumentConfigurationSource
    {
        IDocumentDefinition GetConfigurationByName(string configurationName);
    }
}

