using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Layouts
{
    public class YamlFieldReferenceDefinition : YamlLayoutItemDefinition, IFieldReferenceDefinition
    {
        public string FieldRef { get; set; }
    }
}
