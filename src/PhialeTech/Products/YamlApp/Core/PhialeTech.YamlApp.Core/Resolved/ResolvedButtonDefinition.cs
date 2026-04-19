using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedButtonDefinition : ResolvedLayoutItemDefinition
    {
        public ResolvedButtonDefinition(
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
            string commandId,
            ButtonTone tone,
            ButtonVariant variant,
            ButtonSize size,
            IconPlacement iconPlacement)
            : base(id, name, width, widthHint, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement)
        {
            TextKey = textKey;
            IconKey = iconKey;
            ToolTipKey = toolTipKey;
            CommandId = commandId;
            Tone = tone;
            Variant = variant;
            Size = size;
            IconPlacement = iconPlacement;
        }

        public string TextKey { get; }

        public string IconKey { get; }

        public string ToolTipKey { get; }

        public string CommandId { get; }

        public ButtonTone Tone { get; }

        public ButtonVariant Variant { get; }

        public ButtonSize Size { get; }

        public IconPlacement IconPlacement { get; }
    }
}
