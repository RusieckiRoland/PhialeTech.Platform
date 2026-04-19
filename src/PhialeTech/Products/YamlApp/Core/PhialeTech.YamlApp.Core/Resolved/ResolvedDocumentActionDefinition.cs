using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedDocumentActionDefinition
    {
        public ResolvedDocumentActionDefinition(IDocumentActionDefinition definition, bool visible, bool enabled)
        {
            Definition = definition;
            Visible = visible;
            Enabled = enabled;
        }

        public IDocumentActionDefinition Definition { get; }

        public string Id => Definition == null ? null : Definition.Id;

        public string Name => Definition == null ? null : Definition.Name;

        public string CaptionKey => Definition == null ? null : Definition.CaptionKey;

        public string IconKey => Definition == null ? null : Definition.IconKey;

        public string Area => Definition == null ? null : Definition.Area;

        public bool IsPrimary => Definition != null && (Definition.IsPrimary ?? false);

        public int? Order => Definition == null ? null : Definition.Order;

        public ActionSlot? Slot => Definition == null ? null : Definition.Slot;

        public ActionSemantic? Semantic => Definition == null ? null : Definition.Semantic;

        public DocumentActionKind ActionKind => Definition == null ? default(DocumentActionKind) : Definition.ActionKind;

        public bool Visible { get; }

        public bool Enabled { get; }
    }
}


