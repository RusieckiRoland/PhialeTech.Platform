using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedFieldDefinition
    {
        public ResolvedFieldDefinition(
            IFieldDefinition definition,
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
            Definition = definition;
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

        public IFieldDefinition Definition { get; }

        public string Id => Definition == null ? null : Definition.Id;

        public string Name => Definition == null ? null : Definition.Name;

        public string CaptionKey => Definition == null ? null : Definition.CaptionKey;

        public string PlaceholderKey => Definition == null ? null : Definition.PlaceholderKey;

        public double? Width { get; }

        public FieldWidthHint? WidthHint { get; }

        public double? Weight => Definition == null ? null : Definition.Weight;

        public bool Visible { get; }

        public bool Enabled { get; }

        public bool ShowOldValueRestoreButton { get; }

        public ValidationTrigger ValidationTrigger { get; }

        public InteractionMode InteractionMode { get; }

        public DensityMode? DensityMode { get; }

        public FieldChromeMode FieldChromeMode { get; }

        public CaptionPlacement CaptionPlacement { get; }

        public bool IsRequired => Definition != null && Definition.IsRequired;

        public bool ShowLabel => Definition != null && Definition.ShowLabel;

        public bool ShowPlaceholder => Definition != null && Definition.ShowPlaceholder;

        public int? MaxLength => (Definition as IStringFieldDefinition)?.MaxLength;

        public int? MinValue => (Definition as IIntegerFieldDefinition)?.MinValue;

        public int? MaxNumericValue => (Definition as IIntegerFieldDefinition)?.MaxValue;

        public ResolvedFieldDefinition WithPresentation(
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
            return new ResolvedFieldDefinition(Definition, width, widthHint, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement);
        }
    }
}
