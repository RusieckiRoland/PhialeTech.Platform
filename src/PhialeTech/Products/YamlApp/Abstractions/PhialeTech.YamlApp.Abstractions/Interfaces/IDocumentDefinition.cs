using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDocumentDefinition : IFieldPresentationOptions
    {
        string Id { get; }
        string Name { get; }
        DocumentKind? Kind { get; }
        ILayoutDefinition Layout { get; }
    }
}

