using System.Windows.Input;
using PhialeTech.ComponentHost.Abstractions.Presentation;
using UniversalInput.Contracts;

namespace PhialeTech.ComponentHost.Wpf.Bridges
{
    public static class HostedSurfaceUniversalInputBridge
    {
        public static UniversalCommandEventArgs CreateBackdropCommand()
        {
            return new UniversalCommandEventArgs(HostedSurfaceCommandIds.Backdrop, false, false, false)
            {
                Metadata = CreateMetadata(),
            };
        }

        public static UniversalKeyEventArgs CreateKeyEventArgs(KeyEventArgs e, bool isKeyDown)
        {
            var key = e == null ? string.Empty : (e.Key == Key.System ? e.SystemKey.ToString() : e.Key.ToString());
            return new UniversalKeyEventArgs(key, isKeyDown)
            {
                IsRepeat = e != null && e.IsRepeat,
                Metadata = CreateMetadata(),
            };
        }

        public static UniversalFocusChangedEventArgs CreateFocusChangedEventArgs(bool hasFocus)
        {
            return new UniversalFocusChangedEventArgs(hasFocus)
            {
                Metadata = CreateMetadata(),
            };
        }

        private static UniversalMetadata CreateMetadata()
        {
            return new UniversalMetadata
            {
                Modifiers = ToUniversalModifierKeys(Keyboard.Modifiers),
            };
        }

        private static UniversalModifierKeys ToUniversalModifierKeys(ModifierKeys modifierKeys)
        {
            var result = UniversalModifierKeys.None;
            if ((modifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                result |= UniversalModifierKeys.Shift;
            }

            if ((modifierKeys & ModifierKeys.Control) == ModifierKeys.Control)
            {
                result |= UniversalModifierKeys.Control;
            }

            if ((modifierKeys & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                result |= UniversalModifierKeys.Alt;
            }

            if ((modifierKeys & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                result |= UniversalModifierKeys.Windows;
            }

            return result;
        }
    }
}
