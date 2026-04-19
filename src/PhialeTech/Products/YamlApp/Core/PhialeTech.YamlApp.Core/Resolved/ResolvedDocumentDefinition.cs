using PhialeTech.YamlApp.Abstractions.Enums;
namespace PhialeTech.YamlApp.Core.Resolved
{
    public class ResolvedDocumentDefinition
    {
        public ResolvedDocumentDefinition(
            string id,
            string name,
            DocumentKind? kind,
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
            ResolvedLayoutDefinition layout)
        {
            Id = id;
            Name = name;
            Kind = kind;
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
            Layout = layout;
        }

        public string Id { get; }

        public string Name { get; }

        public DocumentKind? Kind { get; }

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

        public ResolvedLayoutDefinition Layout { get; }
    }
}

