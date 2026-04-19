namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDocumentHeaderDefinition
    {
        string TitleKey { get; }
        string SubtitleKey { get; }
        string DescriptionKey { get; }
        string StatusKey { get; }
        string ContextKey { get; }
        string IconKey { get; }
        bool? Visible { get; }
    }
}
