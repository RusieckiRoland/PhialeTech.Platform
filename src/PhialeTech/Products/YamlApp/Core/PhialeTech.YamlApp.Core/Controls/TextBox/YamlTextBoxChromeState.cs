using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Controls.TextBox
{
    /// <summary>
    /// Derived chrome state consumed by platform renderers.
    /// </summary>
    public sealed class YamlTextBoxChromeState
    {
        public string Caption { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string SupportText { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string ThemeId { get; set; } = "default";

        public bool IsEnabled { get; set; } = true;

        public bool IsReadOnly { get; set; }

        public bool IsRequired { get; set; }

        public bool HasFocus { get; set; }

        public bool HasHover { get; set; }

        public bool HasPressed { get; set; }

        public bool HasError { get; set; }

        public bool ShowClearButton { get; set; }

        public bool ShowRestoreOldValueButton { get; set; }

        public YamlTextBoxTrailingActionKind TrailingActionKind { get; set; }

        public FieldChromeMode FieldChromeMode { get; set; } = FieldChromeMode.Framed;

        public CaptionPlacement CaptionPlacement { get; set; } = CaptionPlacement.Top;

        public DensityMode DensityMode { get; set; } = DensityMode.Normal;

        public InteractionMode InteractionMode { get; set; } = InteractionMode.Classic;

        public YamlTextBoxLayoutMetrics LayoutMetrics { get; set; }

        public bool UsesFramedChrome => FieldChromeMode == FieldChromeMode.Framed;

        public bool UsesInlineChrome => FieldChromeMode == FieldChromeMode.InlineHint;
    }
}
