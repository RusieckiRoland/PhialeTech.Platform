using System.Windows.Input;
using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Wpf.Controls.Badges
{
    internal static class YamlBadgeUniversalInputBridge
    {
        public static UniversalThemeChangedEventArgs CreateThemeChangedEventArgs(string themeId)
        {
            return new UniversalThemeChangedEventArgs(themeId)
            {
                Metadata = CreateMetadata(),
            };
        }

        private static UniversalMetadata CreateMetadata()
        {
            var modifiers = Keyboard.Modifiers;
            var result = UniversalModifierKeys.None;

            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                result |= UniversalModifierKeys.Shift;
            }

            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                result |= UniversalModifierKeys.Control;
            }

            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                result |= UniversalModifierKeys.Alt;
            }

            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                result |= UniversalModifierKeys.Windows;
            }

            return new UniversalMetadata
            {
                Modifiers = result,
            };
        }
    }
}
