using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Fields
{
    public class YamlStringFieldDefinition : YamlValueFieldDefinition<string>, IStringFieldDefinition
    {
        public int? MaxLength { get; set; }
    }
}
