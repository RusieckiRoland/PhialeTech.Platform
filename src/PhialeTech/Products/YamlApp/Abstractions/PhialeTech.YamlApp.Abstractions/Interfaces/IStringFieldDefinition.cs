namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IStringFieldDefinition : IValueFieldDefinition<string>
    {
        int? MaxLength { get; }
    }
}
