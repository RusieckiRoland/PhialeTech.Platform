using System;

namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorSelectionChangedEventArgs : EventArgs
    {
        public DocumentEditorSelectionChangedEventArgs(int from, int to, bool isEmpty)
        {
            From = from;
            To = to;
            IsEmpty = isEmpty;
        }

        public int From { get; }

        public int To { get; }

        public bool IsEmpty { get; }
    }
}
