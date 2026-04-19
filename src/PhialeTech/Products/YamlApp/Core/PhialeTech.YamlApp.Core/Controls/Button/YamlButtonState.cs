using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Controls.Button
{
    public sealed class YamlButtonState
    {
        public string ThemeId { get; set; } = "default";

        public bool IsEnabled { get; set; } = true;

        public bool HasFocus { get; set; }

        public bool HasHover { get; set; }

        public bool HasPressed { get; set; }

        public string CommandId { get; set; } = string.Empty;

        public ButtonTone Tone { get; set; } = ButtonTone.Secondary;

        public ButtonVariant Variant { get; set; } = ButtonVariant.Standard;

        public ButtonSize Size { get; set; } = ButtonSize.Regular;

        public bool HasTextContent { get; set; } = true;
    }
}
