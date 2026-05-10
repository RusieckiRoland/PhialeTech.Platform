using PhialeTech.YamlApp.Abstractions.Enums;
using System.Collections.Generic;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedContainerDefinition : ResolvedLayoutContainerDefinition
    {
        public ResolvedContainerDefinition(
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
            string captionKey,
            string collapsedText,
            ContainerChrome containerChrome,
            ContainerBehavior containerBehavior,
            ContainerVariant variant,
            IReadOnlyList<ResolvedLayoutItemDefinition> items)
            : base(id, name, width, widthHint, isOverlayScope, visible, enabled, showOldValueRestoreButton, validationTrigger, interactionMode, densityMode, fieldChromeMode, captionPlacement, items)
        {
            CaptionKey = captionKey;
            CollapsedText = collapsedText;
            ContainerChrome = containerChrome;
            ContainerBehavior = containerBehavior;
            Variant = variant;
        }

        public string CaptionKey { get; }

        public string CollapsedText { get; }

        public ContainerChrome ContainerChrome { get; }

        public ContainerBehavior ContainerBehavior { get; }

        public ContainerVariant Variant { get; }
    }
}
