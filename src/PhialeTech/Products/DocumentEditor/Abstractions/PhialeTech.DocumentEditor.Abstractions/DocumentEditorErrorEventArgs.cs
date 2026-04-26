using System;

namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorErrorEventArgs : EventArgs
    {
        public DocumentEditorErrorEventArgs(string message, string detail)
        {
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string Message { get; }

        public string Detail { get; }
    }
}
