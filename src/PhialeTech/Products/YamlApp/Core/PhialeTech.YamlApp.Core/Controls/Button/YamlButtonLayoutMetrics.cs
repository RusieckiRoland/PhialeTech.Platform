namespace PhialeTech.YamlApp.Core.Controls.Button
{
    public sealed class YamlButtonLayoutMetrics
    {
        public YamlButtonLayoutMetrics(
            double minimumWidth,
            double minimumHeight,
            double horizontalPadding,
            double verticalPadding,
            double cornerRadius,
            double fontSize,
            double iconSize,
            double contentGap)
        {
            MinimumWidth = minimumWidth;
            MinimumHeight = minimumHeight;
            HorizontalPadding = horizontalPadding;
            VerticalPadding = verticalPadding;
            CornerRadius = cornerRadius;
            FontSize = fontSize;
            IconSize = iconSize;
            ContentGap = contentGap;
        }

        public double MinimumWidth { get; }

        public double MinimumHeight { get; }

        public double HorizontalPadding { get; }

        public double VerticalPadding { get; }

        public double CornerRadius { get; }

        public double FontSize { get; }

        public double IconSize { get; }

        public double ContentGap { get; }
    }
}
