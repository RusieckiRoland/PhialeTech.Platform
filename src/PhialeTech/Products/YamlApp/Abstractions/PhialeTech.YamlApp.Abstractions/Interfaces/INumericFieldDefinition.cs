namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface INumericFieldDefinition<TValue> : IValueFieldDefinition<TValue>
        where TValue : struct
    {
        TValue? MinValue { get; }

        TValue? MaxValue { get; }
    }
}
