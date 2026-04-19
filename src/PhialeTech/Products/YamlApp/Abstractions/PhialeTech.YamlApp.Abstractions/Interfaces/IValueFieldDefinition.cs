namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IValueFieldDefinition<TValue> : IFieldDefinition
    {
        TValue Value { get; }
        TValue OldValue { get; }
    }
}
