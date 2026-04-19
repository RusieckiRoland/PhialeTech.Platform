using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;
using PhialeTech.YamlApp.Definitions.Layouts;

namespace PhialeTech.YamlApp.Definitions.Documents
{
    public class YamlDocumentDefinition : IDocumentDefinition
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DocumentKind? Kind { get; set; }

        public DocumentRegionChromeMode? TopRegionChrome { get; set; }

        public DocumentRegionChromeMode? BottomRegionChrome { get; set; }

        public double? Width { get; set; }

        public FieldWidthHint? WidthHint { get; set; }

        public double? Weight { get; set; }

        public bool? Visible { get; set; }

        public bool? Enabled { get; set; }

        public bool? ShowOldValueRestoreButton { get; set; }

        public ValidationTrigger? ValidationTrigger { get; set; }

        public InteractionMode? InteractionMode { get; set; }

        public DensityMode? DensityMode { get; set; }

        public FieldChromeMode? FieldChromeMode { get; set; }

        public CaptionPlacement? CaptionPlacement { get; set; }

        public YamlDocumentHeaderDefinition Header { get; set; }

        public YamlDocumentFooterDefinition Footer { get; set; }

        public YamlLayoutDefinition Layout { get; set; }

        IDocumentHeaderDefinition IDocumentDefinition.Header => Header;

        IDocumentFooterDefinition IDocumentDefinition.Footer => Footer;

        ILayoutDefinition IDocumentDefinition.Layout => Layout;
    }
}

