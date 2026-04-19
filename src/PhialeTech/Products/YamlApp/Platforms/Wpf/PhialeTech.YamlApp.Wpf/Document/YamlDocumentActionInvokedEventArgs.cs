using System;
using System.Windows;
using PhialeTech.YamlApp.Runtime.Model;

namespace PhialeTech.YamlApp.Wpf.Document
{
    public sealed class YamlDocumentActionInvokedEventArgs : RoutedEventArgs
    {
        public YamlDocumentActionInvokedEventArgs(
            RoutedEvent routedEvent,
            object source,
            RuntimeDocumentState documentState,
            RuntimeActionState actionState)
            : base(routedEvent, source)
        {
            DocumentState = documentState ?? throw new ArgumentNullException(nameof(documentState));
            ActionState = actionState ?? throw new ArgumentNullException(nameof(actionState));
        }

        public RuntimeDocumentState DocumentState { get; }

        public RuntimeActionState ActionState { get; }
    }
}
