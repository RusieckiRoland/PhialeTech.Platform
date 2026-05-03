using PhialeTech.YamlApp.Abstractions.Enums;
using System.Collections.Generic;

namespace PhialeTech.YamlApp.Core.Resolved
{
    public sealed class ResolvedLayoutDefinition
    {
        public ResolvedLayoutDefinition(
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
        {
            Id = id;
            Name = name;
            Width = width;
            WidthHint = widthHint;
            IsOverlayScope = isOverlayScope;
            Visible = visible;
            Enabled = enabled;
            ShowOldValueRestoreButton = showOldValueRestoreButton;
            ValidationTrigger = validationTrigger;
            InteractionMode = interactionMode;
            DensityMode = densityMode;
            FieldChromeMode = fieldChromeMode;
            CaptionPlacement = captionPlacement;
            Items = items;
        }

        public string Id { get; }

        public string Name { get; }

        public double? Width { get; }

        public FieldWidthHint? WidthHint { get; }

        public bool IsOverlayScope { get; }

        public bool Visible { get; }

        public bool Enabled { get; }

        public bool ShowOldValueRestoreButton { get; }

        public ValidationTrigger ValidationTrigger { get; }

        public InteractionMode InteractionMode { get; }

        public DensityMode? DensityMode { get; }

        public FieldChromeMode FieldChromeMode { get; }

        public CaptionPlacement CaptionPlacement { get; }

        public IReadOnlyList<ResolvedLayoutItemDefinition> Items { get; }
    }
}
