using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Fields
{
    public class YamlValueFieldDefinition<TValue> : YamlFieldDefinition, IValueFieldDefinition<TValue>
    {
        public TValue Value { get; set; }

        public TValue OldValue { get; set; }
    }
}
