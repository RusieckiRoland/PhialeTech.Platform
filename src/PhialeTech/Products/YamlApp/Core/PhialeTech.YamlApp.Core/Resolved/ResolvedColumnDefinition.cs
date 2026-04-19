using PhialeTech.YamlApp.Abstractions.Enums;
using System.Collections.Generic;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedColumnDefinition : ResolvedLayoutContainerDefinition
    {
        public ResolvedColumnDefinition(
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
            IReadOnlyList<ResolvedLayoutItemDefinition> items)
            : base(id, name, width, widthHint, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement, items)
        {
        }
    }
}
