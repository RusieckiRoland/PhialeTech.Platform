using System;
using PhialeTech.YamlApp.Abstractions.Enums;
using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Core.Controls.TextBox
{
    /// <summary>
    /// Shared interaction controller for platform-specific YamlTextBox hosts.
    /// </summary>
    public sealed class YamlTextBoxController
    {
        private static readonly YamlTextBoxLayoutMetrics FramedMetrics =
            new YamlTextBoxLayoutMetrics(
                minimumHeight: 94,
                editorLeft: 14,
                editorTop: 32,
                editorRight: 14,
                editorBottom: 26,
                captionWidth: 0,
                clearButtonWidth: 14,
                clearButtonHeight: 14);

        private static readonly YamlTextBoxLayoutMetrics FramedLeftMetrics =
            new YamlTextBoxLayoutMetrics(
                minimumHeight: 94,
                editorLeft: 62,
                editorTop: 32,
                editorRight: 14,
                editorBottom: 26,
                captionWidth: 40,
                clearButtonWidth: 14,
                clearButtonHeight: 14);

        private static readonly YamlTextBoxLayoutMetrics InlineMetrics =
            new YamlTextBoxLayoutMetrics(
                minimumHeight: 94,
                editorLeft: 0,
                editorTop: 32,
                editorRight: 0,
                editorBottom: 26,
                captionWidth: 0,
                clearButtonWidth: 12,
                clearButtonHeight: 12);

        private static readonly YamlTextBoxLayoutMetrics InlineLeftMetrics =
            new YamlTextBoxLayoutMetrics(
                minimumHeight: 94,
                editorLeft: 48,
                editorTop: 32,
                editorRight: 0,
                editorBottom: 26,
                captionWidth: 40,
                clearButtonWidth: 12,
                clearButtonHeight: 12);

        private readonly YamlTextBoxState _state = new YamlTextBoxState();

        public event EventHandler<YamlTextBoxChromeState> StateChanged;

        public YamlTextBoxState State => _state;

        public void SetText(string text)
        {
            text = text ?? string.Empty;
            if (string.Equals(_state.Text, text, StringComparison.Ordinal))
            {
                return;
            }

            _state.Text = text;
            RaiseStateChanged();
        }

        public void SetOldValue(string oldValue)
        {
            oldValue = oldValue ?? string.Empty;
            if (string.Equals(_state.OldValue, oldValue, StringComparison.Ordinal))
            {
                return;
            }

            _state.OldValue = oldValue;
            RaiseStateChanged();
        }

        public void SetCaption(string caption)
        {
            caption = caption ?? string.Empty;
            if (string.Equals(_state.Caption, caption, StringComparison.Ordinal))
            {
                return;
            }

            _state.Caption = caption;
            RaiseStateChanged();
        }

        public void SetPlaceholder(string placeholder)
        {
            placeholder = placeholder ?? string.Empty;
            if (string.Equals(_state.Placeholder, placeholder, StringComparison.Ordinal))
            {
                return;
            }

            _state.Placeholder = placeholder;
            RaiseStateChanged();
        }

        public void SetErrorMessage(string errorMessage)
        {
            errorMessage = errorMessage ?? string.Empty;
            if (string.Equals(_state.ErrorMessage, errorMessage, StringComparison.Ordinal))
            {
                return;
            }

            _state.ErrorMessage = errorMessage;
            RaiseStateChanged();
        }

        public void SetEnabled(bool isEnabled)
        {
            if (_state.IsEnabled == isEnabled)
            {
                return;
            }

            _state.IsEnabled = isEnabled;
            RaiseStateChanged();
        }

        public void SetReadOnly(bool isReadOnly)
        {
            if (_state.IsReadOnly == isReadOnly)
            {
                return;
            }

            _state.IsReadOnly = isReadOnly;
            RaiseStateChanged();
        }

        public void SetRequired(bool isRequired)
        {
            if (_state.IsRequired == isRequired)
            {
                return;
            }

            _state.IsRequired = isRequired;
            RaiseStateChanged();
        }

        public void SetShowOldValueRestoreButton(bool showOldValueRestoreButton)
        {
            if (_state.ShowOldValueRestoreButton == showOldValueRestoreButton)
            {
                return;
            }

            _state.ShowOldValueRestoreButton = showOldValueRestoreButton;
            RaiseStateChanged();
        }

        public void SetHover(bool hasHover)
        {
            if (_state.HasHover == hasHover)
            {
                return;
            }

            _state.HasHover = hasHover;
            RaiseStateChanged();
        }

        public void SetPressed(bool hasPressed)
        {
            if (_state.HasPressed == hasPressed)
            {
                return;
            }

            _state.HasPressed = hasPressed;
            RaiseStateChanged();
        }

        public void SetInteractionMode(InteractionMode interactionMode)
        {
            if (_state.InteractionMode == interactionMode)
            {
                return;
            }

            _state.InteractionMode = interactionMode;
            RaiseStateChanged();
        }

        public void SetChromeMode(FieldChromeMode fieldChromeMode)
        {
            if (_state.FieldChromeMode == fieldChromeMode)
            {
                return;
            }

            _state.FieldChromeMode = fieldChromeMode;
            RaiseStateChanged();
        }

        public void SetCaptionPlacement(CaptionPlacement captionPlacement)
        {
            if (_state.CaptionPlacement == captionPlacement)
            {
                return;
            }

            _state.CaptionPlacement = captionPlacement;
            RaiseStateChanged();
        }

        public void SetDensityMode(DensityMode densityMode)
        {
            if (_state.DensityMode == densityMode)
            {
                return;
            }

            _state.DensityMode = densityMode;
            RaiseStateChanged();
        }

        public void ClearText()
        {
            SetText(string.Empty);
        }

        public void RestoreOldValue()
        {
            SetText(_state.OldValue);
        }

        public void HandleTextChanged(UniversalTextChangedEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            SetText(args.Text);
        }

        public void HandleFocusChanged(UniversalFocusChangedEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            if (_state.HasFocus == args.HasFocus)
            {
                return;
            }

            _state.HasFocus = args.HasFocus;
            RaiseStateChanged();
        }

        public void HandleThemeChanged(UniversalThemeChangedEventArgs args)
        {
            var themeId = args?.ThemeId ?? "default";
            if (string.Equals(_state.ThemeId, themeId, StringComparison.Ordinal))
            {
                return;
            }

            _state.ThemeId = themeId;
            RaiseStateChanged();
        }

        public YamlTextBoxChromeState GetChromeState()
        {
            var hasError = _state.HasError;
            var trailingActionKind = _state.TrailingActionKind;
            return new YamlTextBoxChromeState
            {
                Caption = BuildCaption(),
                Placeholder = _state.Placeholder,
                SupportText = hasError ? _state.ErrorMessage : string.Empty,
                Text = _state.Text,
                ThemeId = _state.ThemeId,
                IsEnabled = _state.IsEnabled,
                IsReadOnly = _state.IsReadOnly,
                IsRequired = _state.IsRequired,
                HasFocus = _state.HasFocus,
                HasHover = _state.HasHover,
                HasPressed = _state.HasPressed,
                HasError = hasError,
                ShowClearButton = _state.ShowClearButton,
                ShowRestoreOldValueButton = _state.ShowRestoreOldValueButton,
                TrailingActionKind = trailingActionKind,
                FieldChromeMode = _state.FieldChromeMode,
                CaptionPlacement = _state.CaptionPlacement,
                DensityMode = _state.DensityMode,
                InteractionMode = _state.InteractionMode,
                LayoutMetrics = ResolveLayoutMetrics(),
            };
        }

        private YamlTextBoxLayoutMetrics ResolveLayoutMetrics()
        {
            if (_state.FieldChromeMode == FieldChromeMode.InlineHint)
            {
                return _state.CaptionPlacement == CaptionPlacement.Left ? InlineLeftMetrics : InlineMetrics;
            }

            return _state.CaptionPlacement == CaptionPlacement.Left ? FramedLeftMetrics : FramedMetrics;
        }

        private string BuildCaption()
        {
            return _state.Caption ?? string.Empty;
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, GetChromeState());
        }
    }
}
