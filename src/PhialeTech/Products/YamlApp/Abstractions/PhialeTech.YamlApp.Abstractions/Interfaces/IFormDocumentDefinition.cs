using System.Collections.Generic;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IFormDocumentDefinition : IDocumentDefinition
    {
        IEnumerable<IActionAreaDefinition> ActionAreas { get; }

        IEnumerable<IFieldDefinition> Fields { get; }

        IEnumerable<IDocumentActionDefinition> Actions { get; }
    }
}
