using System.Collections.Generic;
using System.Windows.Input;
using UniversalInput.Contracts;

namespace PhialeTech.ActiveLayerSelector.Wpf.Controls
{
    internal static class ActiveLayerSelectorUniversalInputAdapter
    {
        public static UniversalCommandEventArgs CreateCommand(
            string commandId,
            ModifierKeys modifiers,
            IDictionary<string, string> arguments = null)
        {
            var args = new UniversalCommandEventArgs(
                commandId,
                (modifiers & ModifierKeys.Control) == ModifierKeys.Control,
                (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt,
                (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Metadata = new UniversalMetadata
                {
                    Modifiers = ConvertModifiers(modifiers),
                },
            };

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    args.Arguments[argument.Key] = argument.Value;
                }
            }

            return args;
        }

        private static UniversalModifierKeys ConvertModifiers(ModifierKeys modifiers)
        {
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

            return result;
        }
    }
}
