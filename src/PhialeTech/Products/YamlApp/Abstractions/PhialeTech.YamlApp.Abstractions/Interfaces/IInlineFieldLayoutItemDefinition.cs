namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IInlineFieldLayoutItemDefinition : ILayoutItemDefinition
    {
        IFieldDefinition Field { get; }
    }
}
