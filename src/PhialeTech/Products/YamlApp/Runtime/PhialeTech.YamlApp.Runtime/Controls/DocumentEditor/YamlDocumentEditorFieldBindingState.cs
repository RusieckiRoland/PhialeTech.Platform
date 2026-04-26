using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Runtime.Controls.DocumentEditor
{
    public sealed class YamlDocumentEditorFieldBindingState
    {
        public string Caption { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string DocumentJson { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public bool IsRequired { get; set; }

        public bool ShowOldValueRestoreButton { get; set; }

        public InteractionMode InteractionMode { get; set; } = InteractionMode.Classic;

        public DensityMode? DensityMode { get; set; }

        public FieldChromeMode FieldChromeMode { get; set; } = FieldChromeMode.Framed;

        public CaptionPlacement CaptionPlacement { get; set; } = CaptionPlacement.Top;
    }
}
