using System.Windows.Input;
using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Wpf.Controls.Buttons
{
    internal static class YamlButtonUniversalInputBridge
    {
        public static UniversalFocusChangedEventArgs CreateFocusChangedEventArgs(bool hasFocus)
        {
            return new UniversalFocusChangedEventArgs(hasFocus)
            {
                Metadata = CreateMetadata(),
            };
        }

        public static UniversalTappedRoutedEventArgs CreateTappedEventArgs()
        {
            return new UniversalTappedRoutedEventArgs(new UniversalPoint
            {
                X = 0d,
                Y = 0d,
            })
            {
                Metadata = CreateMetadata(),
            };
        }

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
