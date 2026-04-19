using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Documents
{
    public class YamlDocumentActionDefinition : IDocumentActionDefinition
    {
        public string Id { get; set; }

        public string Extends { get; set; }

        public string Name { get; set; }

        public string CaptionKey { get; set; }

        public string IconKey { get; set; }

        public string Area { get; set; }

        public bool? IsPrimary { get; set; }

        public int? Order { get; set; }

        public ActionSlot? Slot { get; set; }

        public ActionSemantic? Semantic { get; set; }

        public bool? Visible { get; set; }

        public bool? Enabled { get; set; }

        public DocumentActionKind ActionKind => Semantic.HasValue
            ? (DocumentActionKind)Semantic.Value
            : DocumentActionKind.Custom;
    }
}


