using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IFieldPresentationOptions
    {
        double? Width { get; }
        FieldWidthHint? WidthHint { get; }
        double? Weight { get; }
        bool? Visible { get; }
        bool? Enabled { get; }
        bool? ShowOldValueRestoreButton { get; }
        ValidationTrigger? ValidationTrigger { get; }
        InteractionMode? InteractionMode { get; }
        DensityMode? DensityMode { get; }
        FieldChromeMode? FieldChromeMode { get; }
        CaptionPlacement? CaptionPlacement { get; }
    }
}
