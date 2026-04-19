using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Controls.Badge
{
    public sealed class YamlBadgeState
    {
        public string Text { get; set; } = string.Empty;

        public string ThemeId { get; set; } = "default";

        public bool IsEnabled { get; set; } = true;

        public BadgeTone Tone { get; set; } = BadgeTone.Neutral;

        public BadgeVariant Variant { get; set; } = BadgeVariant.Soft;

        public BadgeSize Size { get; set; } = BadgeSize.Regular;
    }
}
