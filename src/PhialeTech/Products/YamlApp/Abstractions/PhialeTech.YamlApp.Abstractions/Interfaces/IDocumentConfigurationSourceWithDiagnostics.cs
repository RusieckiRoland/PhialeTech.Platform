using System.Collections.Generic;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDocumentConfigurationSourceWithDiagnostics : IDocumentConfigurationSource
    {
        bool TryGetConfigurationByName(string configurationName, out IDocumentDefinition configuration, out IReadOnlyList<string> diagnostics);
    }
}

