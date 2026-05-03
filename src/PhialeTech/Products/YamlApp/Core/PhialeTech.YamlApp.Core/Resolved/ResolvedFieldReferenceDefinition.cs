using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedFieldReferenceDefinition : ResolvedLayoutItemDefinition
    {
        public ResolvedFieldReferenceDefinition(
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
            ResolvedFieldDefinition field)
            : base(id, name, width, widthHint, false, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement)
        {
            Field = field;
        }

        public ResolvedFieldDefinition Field { get; }

        public string FieldRef => Field == null ? null : Field.Id;
    }
}
