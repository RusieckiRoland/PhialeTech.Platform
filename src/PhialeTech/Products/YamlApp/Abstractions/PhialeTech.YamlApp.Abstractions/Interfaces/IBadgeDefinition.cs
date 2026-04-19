using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IBadgeDefinition : ILayoutItemDefinition, IFieldPresentationOptions
    {
        string TextKey { get; }

        string IconKey { get; }

        string ToolTipKey { get; }

        BadgeTone? Tone { get; }

        BadgeVariant? Variant { get; }

        BadgeSize? Size { get; }

        IconPlacement? IconPlacement { get; }
    }
}
