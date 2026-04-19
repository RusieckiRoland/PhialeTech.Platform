namespace PhialeTech.YamlApp.Core.Controls.Badge
{
    public sealed class YamlBadgeLayoutMetrics
    {
        public YamlBadgeLayoutMetrics(
            double minimumHeight,
            double horizontalPadding,
            double verticalPadding,
            double cornerRadius,
            double fontSize,
            double iconSize,
            double contentGap)
        {
            MinimumHeight = minimumHeight;
            HorizontalPadding = horizontalPadding;
            VerticalPadding = verticalPadding;
            CornerRadius = cornerRadius;
            FontSize = fontSize;
            IconSize = iconSize;
            ContentGap = contentGap;
        }

        public double MinimumHeight { get; }

        public double HorizontalPadding { get; }

        public double VerticalPadding { get; }

        public double CornerRadius { get; }

        public double FontSize { get; }

        public double IconSize { get; }

        public double ContentGap { get; }
    }
}
