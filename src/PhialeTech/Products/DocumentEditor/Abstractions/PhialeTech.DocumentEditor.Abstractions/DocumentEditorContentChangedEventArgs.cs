using System;

namespace PhialeTech.DocumentEditor.Abstractions
{
    public sealed class DocumentEditorContentChangedEventArgs : EventArgs
    {
        public DocumentEditorContentChangedEventArgs(string html, string markdown, string documentJson, DocumentEditorState state)
        {
            Html = html ?? string.Empty;
            Markdown = markdown ?? string.Empty;
            DocumentJson = documentJson ?? string.Empty;
            State = state ?? new DocumentEditorState();
        }

        public string Html { get; }

        public string Markdown { get; }

        public string DocumentJson { get; }

        public DocumentEditorState State { get; }
    }
}
