using System.Windows;
using UniversalInput.Contracts;

namespace PhialeTech.YamlApp.Wpf.Controls.Buttons
{
    public sealed class YamlButtonInvokedEventArgs : RoutedEventArgs
    {
        public YamlButtonInvokedEventArgs(RoutedEvent routedEvent, object source, UniversalCommandEventArgs command)
            : base(routedEvent, source)
        {
            Command = command;
        }

        public UniversalCommandEventArgs Command { get; }

        public string CommandId => Command == null ? null : Command.CommandId;
    }
}
