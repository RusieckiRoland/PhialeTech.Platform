using System;
using System.Windows;
using UniversalInput.Contracts;

namespace PhialeTech.Shell.Wpf.Input
{
    public sealed class ShellCommandInvokedRoutedEventArgs : RoutedEventArgs
    {
        public ShellCommandInvokedRoutedEventArgs(RoutedEvent routedEvent, object source, UniversalCommandEventArgs command)
            : base(routedEvent, source)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public UniversalCommandEventArgs Command { get; }
    }
}
