using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Documents
{
    public class YamlFormDocumentDefinition : YamlDocumentDefinition, IFormDocumentDefinition
    {
        public List<IActionAreaDefinition> ActionAreas { get; set; } = new List<IActionAreaDefinition>();

        public List<IFieldDefinition> Fields { get; set; } = new List<IFieldDefinition>();

        public List<IDocumentActionDefinition> Actions { get; set; } = new List<IDocumentActionDefinition>();

        IEnumerable<IActionAreaDefinition> IFormDocumentDefinition.ActionAreas => ActionAreas;

        IEnumerable<IFieldDefinition> IFormDocumentDefinition.Fields => Fields;

        IEnumerable<IDocumentActionDefinition> IFormDocumentDefinition.Actions => Actions;
    }
}
