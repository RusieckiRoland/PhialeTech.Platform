using System;
using PhialeTech.YamlApp.Abstractions.Enums;
using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Core.Controls.Button
{
    public sealed class YamlButtonController
    {
        private static readonly YamlButtonLayoutMetrics StandardCompactMetrics =
            new YamlButtonLayoutMetrics(84d, 32d, 12d, 7d, 8d, 12d, 11d, 6d);

        private static readonly YamlButtonLayoutMetrics StandardRegularMetrics =
            new YamlButtonLayoutMetrics(96d, 36d, 16d, 9d, 8d, 13d, 12d, 8d);

        private static readonly YamlButtonLayoutMetrics StandardLargeMetrics =
            new YamlButtonLayoutMetrics(108d, 42d, 18d, 10d, 9d, 14d, 13d, 8d);

        private static readonly YamlButtonLayoutMetrics ToolbarCompactMetrics =
            new YamlButtonLayoutMetrics(0d, 28d, 8d, 5d, 7d, 11d, 10d, 6d);

        private static readonly YamlButtonLayoutMetrics ToolbarRegularMetrics =
            new YamlButtonLayoutMetrics(0d, 32d, 10d, 6d, 7d, 12d, 11d, 6d);

        private static readonly YamlButtonLayoutMetrics ToolbarLargeMetrics =
            new YamlButtonLayoutMetrics(0d, 36d, 12d, 7d, 8d, 13d, 12d, 7d);

        private static readonly YamlButtonLayoutMetrics ActionStripCompactMetrics =
            new YamlButtonLayoutMetrics(96d, 34d, 14d, 8d, 8d, 12d, 11d, 6d);

        private static readonly YamlButtonLayoutMetrics ActionStripRegularMetrics =
            new YamlButtonLayoutMetrics(104d, 38d, 16d, 9d, 8d, 13d, 12d, 8d);

        private static readonly YamlButtonLayoutMetrics ActionStripLargeMetrics =
            new YamlButtonLayoutMetrics(116d, 42d, 18d, 10d, 9d, 14d, 13d, 8d);

        private static readonly YamlButtonLayoutMetrics IconOnlyCompactMetrics =
            new YamlButtonLayoutMetrics(28d, 28d, 8d, 5d, 7d, 11d, 10d, 0d);

        private static readonly YamlButtonLayoutMetrics IconOnlyRegularMetrics =
            new YamlButtonLayoutMetrics(32d, 32d, 10d, 6d, 7d, 12d, 11d, 0d);

        private static readonly YamlButtonLayoutMetrics IconOnlyLargeMetrics =
            new YamlButtonLayoutMetrics(36d, 36d, 11d, 7d, 8d, 13d, 12d, 0d);

        private readonly YamlButtonState _state = new YamlButtonState();

        public event EventHandler<YamlButtonChromeState> StateChanged;

        public YamlButtonState State => _state;

        public void SetEnabled(bool isEnabled)
        {
            if (_state.IsEnabled == isEnabled)
            {
                return;
            }

            _state.IsEnabled = isEnabled;
            RaiseStateChanged();
        }

        public void SetFocus(bool hasFocus)
        {
            if (_state.HasFocus == hasFocus)
            {
                return;
            }

            _state.HasFocus = hasFocus;
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

        public void SetCommandId(string commandId)
        {
            commandId = commandId ?? string.Empty;
            if (string.Equals(_state.CommandId, commandId, StringComparison.Ordinal))
            {
                return;
            }

            _state.CommandId = commandId;
            RaiseStateChanged();
        }

        public void SetTone(ButtonTone tone)
        {
            if (_state.Tone == tone)
            {
                return;
            }

            _state.Tone = tone;
            RaiseStateChanged();
        }

        public void SetVariant(ButtonVariant variant)
        {
            if (_state.Variant == variant)
            {
                return;
            }

            _state.Variant = variant;
            RaiseStateChanged();
        }

        public void SetSize(ButtonSize size)
        {
            if (_state.Size == size)
            {
                return;
            }

            _state.Size = size;
            RaiseStateChanged();
        }

        public void SetHasTextContent(bool hasTextContent)
        {
            if (_state.HasTextContent == hasTextContent)
            {
                return;
            }

            _state.HasTextContent = hasTextContent;
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

        public void HandleFocusChanged(UniversalFocusChangedEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            SetFocus(args.HasFocus);
        }

        public UniversalCommandEventArgs HandleTapped(UniversalTappedRoutedEventArgs args)
        {
            if (!_state.IsEnabled || string.IsNullOrWhiteSpace(_state.CommandId))
            {
                return null;
            }

            var metadata = args?.Metadata;
            var modifiers = metadata == null ? UniversalModifierKeys.None : metadata.Modifiers;
            return new UniversalCommandEventArgs(
                _state.CommandId,
                (modifiers & UniversalModifierKeys.Control) == UniversalModifierKeys.Control,
                (modifiers & UniversalModifierKeys.Alt) == UniversalModifierKeys.Alt,
                (modifiers & UniversalModifierKeys.Shift) == UniversalModifierKeys.Shift)
            {
                Metadata = metadata
            };
        }

        public YamlButtonChromeState GetChromeState()
        {
            return new YamlButtonChromeState
            {
                ThemeId = _state.ThemeId,
                IsEnabled = _state.IsEnabled,
                HasFocus = _state.HasFocus,
                HasHover = _state.HasHover,
                HasPressed = _state.HasPressed,
                CommandId = _state.CommandId,
                Tone = _state.Tone,
                Variant = _state.Variant,
                Size = _state.Size,
                HasTextContent = _state.HasTextContent,
                LayoutMetrics = ResolveLayoutMetrics(_state.Variant, _state.Size, _state.HasTextContent)
            };
        }

        private static YamlButtonLayoutMetrics ResolveLayoutMetrics(ButtonVariant variant, ButtonSize size, bool hasTextContent)
        {
            if (!hasTextContent)
            {
                switch (size)
                {
                    case ButtonSize.Compact:
                        return IconOnlyCompactMetrics;
                    case ButtonSize.Large:
                        return IconOnlyLargeMetrics;
                    default:
                        return IconOnlyRegularMetrics;
                }
            }

            switch (variant)
            {
                case ButtonVariant.Toolbar:
                    switch (size)
                    {
                        case ButtonSize.Compact:
                            return ToolbarCompactMetrics;
                        case ButtonSize.Large:
                            return ToolbarLargeMetrics;
                        default:
                            return ToolbarRegularMetrics;
                    }
                case ButtonVariant.ActionStrip:
                    switch (size)
                    {
                        case ButtonSize.Compact:
                            return ActionStripCompactMetrics;
                        case ButtonSize.Large:
                            return ActionStripLargeMetrics;
                        default:
                            return ActionStripRegularMetrics;
                    }
                default:
                    switch (size)
                    {
                        case ButtonSize.Compact:
                            return StandardCompactMetrics;
                        case ButtonSize.Large:
                            return StandardLargeMetrics;
                        default:
                            return StandardRegularMetrics;
                    }
            }
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, GetChromeState());
        }
    }
}
