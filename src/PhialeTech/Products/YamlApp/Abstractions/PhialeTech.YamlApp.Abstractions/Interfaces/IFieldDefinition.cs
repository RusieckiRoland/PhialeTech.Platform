using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IFieldDefinition : IFieldState, IFieldPresentationOptions
    {
        string Id { get; }
        string Name { get; }
        string Extends { get; }
        string CaptionKey { get; }
        string PlaceholderKey { get; }

        bool IsRequired { get; }
        bool ShowLabel { get; }
        bool ShowPlaceholder { get; }
    }
}
