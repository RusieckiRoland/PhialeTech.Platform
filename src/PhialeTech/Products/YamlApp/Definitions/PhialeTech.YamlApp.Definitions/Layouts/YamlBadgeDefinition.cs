using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Layouts
{
    public class YamlBadgeDefinition : YamlLayoutItemDefinition, IBadgeDefinition
    {
        public string TextKey { get; set; }

        public string IconKey { get; set; }

        public string ToolTipKey { get; set; }

        public BadgeTone? Tone { get; set; }

        public BadgeVariant? Variant { get; set; }

        public BadgeSize? Size { get; set; }

        public IconPlacement? IconPlacement { get; set; }

        public double? Width { get; set; }

        public FieldWidthHint? WidthHint { get; set; }

        public double? Weight { get; set; }

        public bool? Visible { get; set; }

        public bool? Enabled { get; set; }

        public bool? ShowOldValueRestoreButton { get; set; }

        public ValidationTrigger? ValidationTrigger { get; set; }

        public InteractionMode? InteractionMode { get; set; }

        public DensityMode? DensityMode { get; set; }

        public FieldChromeMode? FieldChromeMode { get; set; }

        public CaptionPlacement? CaptionPlacement { get; set; }
    }
}
