using System.Collections.Generic;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface ILayoutDefinition : IFieldPresentationOptions, IOverlayScopeOptions
    {
        string Id { get; }
        string Name { get; }

        IEnumerable<ILayoutItemDefinition> Items { get; }
    }
}
