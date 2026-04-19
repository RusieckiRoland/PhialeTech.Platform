using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Runtime.Controls.TextBox
{
    /// <summary>
    /// Platform-neutral snapshot consumed by platform text box hosts.
    /// </summary>
    public sealed class YamlTextBoxFieldBindingState
    {
        public string FieldId { get; set; } = string.Empty;

        public string Caption { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public double? Width { get; set; }

        public FieldWidthHint? WidthHint { get; set; }

        public int? MaxLength { get; set; }

        public InteractionMode InteractionMode { get; set; } = InteractionMode.Classic;

        public DensityMode? DensityMode { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public bool IsRequired { get; set; }

        public bool ShowOldValueRestoreButton { get; set; }

        public FieldChromeMode FieldChromeMode { get; set; } = FieldChromeMode.Framed;

        public CaptionPlacement CaptionPlacement { get; set; } = CaptionPlacement.Top;
    }
}
