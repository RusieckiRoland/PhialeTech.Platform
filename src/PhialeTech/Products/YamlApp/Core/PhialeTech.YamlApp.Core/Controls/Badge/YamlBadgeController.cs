using System;
using PhialeTech.YamlApp.Abstractions.Enums;
using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Core.Controls.Badge
{
    public sealed class YamlBadgeController
    {
        private static readonly YamlBadgeLayoutMetrics CompactMetrics =
            new YamlBadgeLayoutMetrics(22d, 10d, 4d, 999d, 11d, 10d, 5d);

        private static readonly YamlBadgeLayoutMetrics RegularMetrics =
            new YamlBadgeLayoutMetrics(26d, 12d, 5d, 999d, 12d, 11d, 6d);

        private readonly YamlBadgeState _state = new YamlBadgeState();

        public event EventHandler<YamlBadgeChromeState> StateChanged;

        public YamlBadgeState State => _state;

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

        public void SetTone(BadgeTone tone)
        {
            if (_state.Tone == tone)
            {
                return;
            }

            _state.Tone = tone;
            RaiseStateChanged();
        }

        public void SetVariant(BadgeVariant variant)
        {
            if (_state.Variant == variant)
            {
                return;
            }

            _state.Variant = variant;
            RaiseStateChanged();
        }

        public void SetSize(BadgeSize size)
        {
            if (_state.Size == size)
            {
                return;
            }

            _state.Size = size;
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

        public YamlBadgeChromeState GetChromeState()
        {
            return new YamlBadgeChromeState
            {
                Text = _state.Text,
                ThemeId = _state.ThemeId,
                IsEnabled = _state.IsEnabled,
                Tone = _state.Tone,
                Variant = _state.Variant,
                Size = _state.Size,
                LayoutMetrics = _state.Size == BadgeSize.Compact ? CompactMetrics : RegularMetrics
            };
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, GetChromeState());
        }
    }
}
