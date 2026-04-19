using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Documents
{
    public sealed class YamlDocumentHeaderDefinition : IDocumentHeaderDefinition
    {
        public string TitleKey { get; set; }

        public string SubtitleKey { get; set; }

        public string DescriptionKey { get; set; }

        public string StatusKey { get; set; }

        public string ContextKey { get; set; }

        public string IconKey { get; set; }

        public bool? Visible { get; set; }
    }
}
