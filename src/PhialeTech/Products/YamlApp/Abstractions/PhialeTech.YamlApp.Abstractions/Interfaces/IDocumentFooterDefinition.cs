namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IDocumentFooterDefinition
    {
        string NoteKey { get; }
        string StatusKey { get; }
        string SourceKey { get; }
        bool? Visible { get; }
    }
}
