using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IActionAreaDefinition
    {
        string Id { get; }
        string Extends { get; }
        string Name { get; }
        ActionPlacement? Placement { get; }
        ActionAlignment? HorizontalAlignment { get; }
        ActionAreaChromeMode? ChromeMode { get; }
        bool? Shared { get; }
        bool? Sticky { get; }
        bool? Visible { get; }
    }
}
