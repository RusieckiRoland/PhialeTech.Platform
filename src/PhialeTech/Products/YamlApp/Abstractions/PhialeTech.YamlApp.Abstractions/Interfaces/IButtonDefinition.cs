using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IButtonDefinition : ILayoutItemDefinition, IFieldPresentationOptions
    {
        string TextKey { get; }

        string IconKey { get; }

        string ToolTipKey { get; }

        string CommandId { get; }

        ButtonTone? Tone { get; }

        ButtonVariant? Variant { get; }

        ButtonSize? Size { get; }

        IconPlacement? IconPlacement { get; }
    }
}
