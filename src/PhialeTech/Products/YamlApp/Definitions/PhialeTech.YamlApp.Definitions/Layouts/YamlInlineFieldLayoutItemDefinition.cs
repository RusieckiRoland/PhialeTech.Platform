using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Layouts
{
    public class YamlInlineFieldLayoutItemDefinition : YamlLayoutItemDefinition, IInlineFieldLayoutItemDefinition
    {
        public IFieldDefinition Field { get; set; }
    }
}
