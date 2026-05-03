using PhialeTech.DocumentEditor.Abstractions;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Fields
{
    public sealed class YamlDocumentEditorFieldDefinition : YamlValueFieldDefinition<string>, IDocumentEditorFieldDefinition
    {
        public DocumentEditorOverlayMode? OverlayMode { get; set; }
    }
}
