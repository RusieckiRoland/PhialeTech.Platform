using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Documents
{
    public sealed class YamlDocumentFooterDefinition : IDocumentFooterDefinition
    {
        public string NoteKey { get; set; }

        public string StatusKey { get; set; }

        public string SourceKey { get; set; }

        public bool? Visible { get; set; }
    }
}
