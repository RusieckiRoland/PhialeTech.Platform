using PhialeTech.YamlApp.Abstractions.Enums;
namespace PhialeTech.YamlApp.Core.Resolved
{
    public class ResolvedDocumentDefinition
    {
        public ResolvedDocumentDefinition(
            string id,
            string name,
            DocumentKind? kind,
            DocumentRegionChromeMode topRegionChrome,
            DocumentRegionChromeMode bottomRegionChrome,
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
            ResolvedDocumentHeaderDefinition header,
            ResolvedDocumentFooterDefinition footer,
            ResolvedLayoutDefinition layout)
        {
            Id = id;
            Name = name;
            Kind = kind;
            TopRegionChrome = topRegionChrome;
            BottomRegionChrome = bottomRegionChrome;
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
            Header = header;
            Footer = footer;
            Layout = layout;
        }

        public string Id { get; }

        public string Name { get; }

        public DocumentKind? Kind { get; }

        public DocumentRegionChromeMode TopRegionChrome { get; }

        public DocumentRegionChromeMode BottomRegionChrome { get; }

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

        public ResolvedDocumentHeaderDefinition Header { get; }

        public ResolvedDocumentFooterDefinition Footer { get; }

        public ResolvedLayoutDefinition Layout { get; }
    }
}

