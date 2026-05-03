using PhialeTech.YamlApp.Abstractions.Enums;
using System.Collections.Generic;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public abstract class ResolvedLayoutContainerDefinition : ResolvedLayoutItemDefinition
    {
        protected ResolvedLayoutContainerDefinition(
            string id,
            string name,
            double? width,
            FieldWidthHint? widthHint,
            bool isOverlayScope,
            bool visible,
            bool enabled,
            bool showOldValueRestoreButton,
            ValidationTrigger validationTrigger,
            InteractionMode interactionMode,
            DensityMode? densityMode,
            FieldChromeMode fieldChromeMode,
            CaptionPlacement captionPlacement,
            IReadOnlyList<ResolvedLayoutItemDefinition> items)
            : base(id, name, width, widthHint, isOverlayScope, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement)
        {
            Items = items;
        }

        public IReadOnlyList<ResolvedLayoutItemDefinition> Items { get; }
    }
}
