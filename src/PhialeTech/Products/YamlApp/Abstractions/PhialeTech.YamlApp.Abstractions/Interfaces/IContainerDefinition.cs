using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IContainerDefinition : ILayoutContainerDefinition
    {
        string CaptionKey { get; }

        string CollapsedText { get; }

        ContainerChrome ContainerChrome { get; }

        ContainerBehavior ContainerBehavior { get; }

        ContainerVariant? Variant { get; }
    }
}
