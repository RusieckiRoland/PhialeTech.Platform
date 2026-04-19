using System;

namespace PhialeTech.MonacoEditor.Abstractions
{
    public sealed class MonacoEditorErrorEventArgs : EventArgs
    {
        public MonacoEditorErrorEventArgs(string message, string detail = null)
        {
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Message { get; }

        public string Detail { get; }
    }
}
