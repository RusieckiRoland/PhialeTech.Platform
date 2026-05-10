using System.Collections.Generic;
using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface ILayoutDefinition : IFieldPresentationOptions, IOverlayScopeOptions
    {
        string Id { get; }
        string Name { get; }
        LayoutHeightMode? HeightMode { get; }

        IEnumerable<ILayoutItemDefinition> Items { get; }
    }
}
