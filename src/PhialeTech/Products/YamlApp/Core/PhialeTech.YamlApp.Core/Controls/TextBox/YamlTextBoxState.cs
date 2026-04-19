using PhialeTech.YamlApp.Abstractions.Enums;

namespace PhialeTech.YamlApp.Core.Controls.TextBox
{
    /// <summary>
    /// Platform-agnostic interaction state of the YamlTextBox control.
    /// </summary>
    public sealed class YamlTextBoxState
    {
        public string Text { get; set; } = string.Empty;

        public string OldValue { get; set; } = string.Empty;

        public string Caption { get; set; } = string.Empty;

        public string Placeholder { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public string ThemeId { get; set; } = "default";

        public bool IsEnabled { get; set; } = true;

        public bool IsReadOnly { get; set; }

        public bool IsRequired { get; set; }

        public bool ShowOldValueRestoreButton { get; set; }

        public bool HasFocus { get; set; }

        public bool HasHover { get; set; }

        public bool HasPressed { get; set; }

        public InteractionMode InteractionMode { get; set; } = InteractionMode.Classic;

        public FieldChromeMode FieldChromeMode { get; set; } = FieldChromeMode.Framed;

        public CaptionPlacement CaptionPlacement { get; set; } = CaptionPlacement.Top;

        public DensityMode DensityMode { get; set; } = DensityMode.Normal;

        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        public bool HasOldValue => !string.IsNullOrWhiteSpace(OldValue);

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage) && (!IsRequired || !HasText);

        public YamlTextBoxTrailingActionKind TrailingActionKind
        {
            get
            {
                if (!IsEnabled || IsReadOnly)
                {
                    return YamlTextBoxTrailingActionKind.None;
                }

                if (HasText)
                {
                    return YamlTextBoxTrailingActionKind.Clear;
                }

                if (ShowOldValueRestoreButton && HasOldValue)
                {
                    return YamlTextBoxTrailingActionKind.RestoreOldValue;
                }

                return YamlTextBoxTrailingActionKind.None;
            }
        }

        public bool ShowClearButton => TrailingActionKind == YamlTextBoxTrailingActionKind.Clear;

        public bool ShowRestoreOldValueButton => TrailingActionKind == YamlTextBoxTrailingActionKind.RestoreOldValue;
    }
}
