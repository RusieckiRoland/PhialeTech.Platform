using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedDocumentFooterDefinition
    {
        public ResolvedDocumentFooterDefinition(IDocumentFooterDefinition definition, bool visible)
        {
            Definition = definition;
            Visible = visible;
        }

        public IDocumentFooterDefinition Definition { get; }

        public string NoteKey => Definition == null ? null : Definition.NoteKey;

        public string StatusKey => Definition == null ? null : Definition.StatusKey;

        public string SourceKey => Definition == null ? null : Definition.SourceKey;

        public bool Visible { get; }
    }
}
