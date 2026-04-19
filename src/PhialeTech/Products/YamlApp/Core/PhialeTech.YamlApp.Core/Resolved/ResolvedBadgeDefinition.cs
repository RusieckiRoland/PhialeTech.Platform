using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedBadgeDefinition : ResolvedLayoutItemDefinition
    {
        public ResolvedBadgeDefinition(
            string id,
            string name,
            double? width,
            FieldWidthHint? widthHint,
            bool visible,
            bool enabled,
            bool showOldValueRestoreButton,
            ValidationTrigger validationTrigger,
            InteractionMode interactionMode,
            DensityMode? densityMode,
            FieldChromeMode fieldChromeMode,
            CaptionPlacement captionPlacement,
            string textKey,
            string iconKey,
            string toolTipKey,
            BadgeTone tone,
            BadgeVariant variant,
            BadgeSize size,
            IconPlacement iconPlacement)
            : base(id, name, width, widthHint, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement)
        {
            TextKey = textKey;
            IconKey = iconKey;
            ToolTipKey = toolTipKey;
            Tone = tone;
            Variant = variant;
            Size = size;
            IconPlacement = iconPlacement;
        }

        public string TextKey { get; }

        public string IconKey { get; }

        public string ToolTipKey { get; }

        public BadgeTone Tone { get; }

        public BadgeVariant Variant { get; }

        public BadgeSize Size { get; }

        public IconPlacement IconPlacement { get; }
    }
}
