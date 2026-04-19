namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IContainerDefinition : ILayoutContainerDefinition
    {
        string CaptionKey { get; }

        bool ShowBorder { get; }
    }
}
