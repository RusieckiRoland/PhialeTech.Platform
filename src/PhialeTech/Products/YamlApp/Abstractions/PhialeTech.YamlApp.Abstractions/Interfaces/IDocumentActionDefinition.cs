using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDocumentActionDefinition
    {
        string Id { get; }
        string Extends { get; }
        string Name { get; }
        string CaptionKey { get; }
        string IconKey { get; }
        string Area { get; }
        bool? IsPrimary { get; }
        int? Order { get; }
        ActionSlot? Slot { get; }
        ActionSemantic? Semantic { get; }

        bool? Visible { get; }
        bool? Enabled { get; }

        DocumentActionKind ActionKind { get; }
    }
}


