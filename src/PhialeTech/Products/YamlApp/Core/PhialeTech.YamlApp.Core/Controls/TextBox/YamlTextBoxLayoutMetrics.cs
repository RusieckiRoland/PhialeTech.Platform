namespace PhialeTech.YamlApp.Core.Controls.TextBox
{
    /// <summary>
    /// Platform-agnostic layout metrics used by platform hosts to place the native editor
    /// inside the chrome rendered around it.
    /// </summary>
    public sealed class YamlTextBoxLayoutMetrics
    {
        public YamlTextBoxLayoutMetrics(
            double minimumHeight,
            double editorLeft,
            double editorTop,
            double editorRight,
            double editorBottom,
            double captionWidth,
            double clearButtonWidth,
            double clearButtonHeight)
        {
            MinimumHeight = minimumHeight;
            EditorLeft = editorLeft;
            EditorTop = editorTop;
            EditorRight = editorRight;
            EditorBottom = editorBottom;
            CaptionWidth = captionWidth;
            ClearButtonWidth = clearButtonWidth;
            ClearButtonHeight = clearButtonHeight;
        }

        public double MinimumHeight { get; }

        public double EditorLeft { get; }

        public double EditorTop { get; }

        public double EditorRight { get; }

        public double EditorBottom { get; }

        public double CaptionWidth { get; }

        public double ClearButtonWidth { get; }

        public double ClearButtonHeight { get; }
    }
}
