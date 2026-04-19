using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public abstract class ResolvedLayoutItemDefinition
    {
        protected ResolvedLayoutItemDefinition(
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
            CaptionPlacement captionPlacement = CaptionPlacement.Top)
        {
            Id = id;
            Name = name;
            Width = width;
            WidthHint = widthHint;
            Visible = visible;
            Enabled = enabled;
            ShowOldValueRestoreButton = showOldValueRestoreButton;
            ValidationTrigger = validationTrigger;
            InteractionMode = interactionMode;
            DensityMode = densityMode;
            FieldChromeMode = fieldChromeMode;
            CaptionPlacement = captionPlacement;
        }

        public string Id { get; }

        public string Name { get; }

        public double? Width { get; }

        public FieldWidthHint? WidthHint { get; }

        public bool Visible { get; }

        public bool Enabled { get; }

        public bool ShowOldValueRestoreButton { get; }

        public ValidationTrigger ValidationTrigger { get; }

        public InteractionMode InteractionMode { get; }

        public DensityMode? DensityMode { get; }

        public FieldChromeMode FieldChromeMode { get; }

        public CaptionPlacement CaptionPlacement { get; }
    }
}
