using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedDocumentHeaderDefinition
    {
        public ResolvedDocumentHeaderDefinition(IDocumentHeaderDefinition definition, bool visible)
        {
            Definition = definition;
            Visible = visible;
        }

        public IDocumentHeaderDefinition Definition { get; }

        public string TitleKey => Definition == null ? null : Definition.TitleKey;

        public string SubtitleKey => Definition == null ? null : Definition.SubtitleKey;

        public string DescriptionKey => Definition == null ? null : Definition.DescriptionKey;

        public string StatusKey => Definition == null ? null : Definition.StatusKey;

        public string ContextKey => Definition == null ? null : Definition.ContextKey;

        public string IconKey => Definition == null ? null : Definition.IconKey;

        public bool Visible { get; }
    }
}
