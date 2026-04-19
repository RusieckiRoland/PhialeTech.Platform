using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Fields
{
    public class YamlIntegerFieldDefinition : YamlValueFieldDefinition<int>, IIntegerFieldDefinition
    {
        public int? MinValue { get; set; }

        public int? MaxValue { get; set; }
    }
}
