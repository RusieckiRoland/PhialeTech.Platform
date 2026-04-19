using PhialeTech.YamlApp.Abstractions.Enums;
using PhialeTech.YamlApp.Abstractions.Interfaces;

namespace PhialeTech.YamlApp.Definitions.Fields
{
    public class YamlFieldDefinition : IFieldDefinition
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Extends { get; set; }

        public string CaptionKey { get; set; }

        public string PlaceholderKey { get; set; }

        public double? Width { get; set; }

        public FieldWidthHint? WidthHint { get; set; }

        public double? Weight { get; set; }

        public bool? Visible { get; set; }

        public bool? Enabled { get; set; }

        public bool? ShowOldValueRestoreButton { get; set; }

        public bool IsRequired { get; set; }

        public bool ShowLabel { get; set; }

        public bool ShowPlaceholder { get; set; }

        public ValidationTrigger? ValidationTrigger { get; set; }

        public InteractionMode? InteractionMode { get; set; }

        public DensityMode? DensityMode { get; set; }

        public FieldChromeMode? FieldChromeMode { get; set; }

        public CaptionPlacement? CaptionPlacement { get; set; }

        public bool IsTouched { get; set; }

        public bool IsPristine { get; set; }

        public bool IsDirty { get; set; }

        public bool IsValid { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }
    }
}
