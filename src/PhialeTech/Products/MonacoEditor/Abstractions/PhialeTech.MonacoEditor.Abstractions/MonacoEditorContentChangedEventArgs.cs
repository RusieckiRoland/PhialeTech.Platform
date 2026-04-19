using System;

namespace PhialeTech.MonacoEditor.Abstractions
{
    public sealed class MonacoEditorContentChangedEventArgs : EventArgs
    {
        public MonacoEditorContentChangedEventArgs(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
    }
}
