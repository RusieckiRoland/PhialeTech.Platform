using System.Collections.Generic;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface ILayoutContainerDefinition : ILayoutItemDefinition, IFieldPresentationOptions, IOverlayScopeOptions
    {
        IEnumerable<ILayoutItemDefinition> Items { get; }
    }
}
