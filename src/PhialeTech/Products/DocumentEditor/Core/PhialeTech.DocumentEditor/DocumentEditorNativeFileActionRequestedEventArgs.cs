using System;

namespace PhialeTech.DocumentEditor
{
    public sealed class DocumentEditorNativeFileActionRequestedEventArgs : EventArgs
    {
        public DocumentEditorNativeFileActionRequestedEventArgs(
            DocumentEditorNativeFileActionKind kind,
            string html,
            string markdown,
            string documentJson)
        {
            Kind = kind;
            Html = html ?? string.Empty;
            Markdown = markdown ?? string.Empty;
            DocumentJson = documentJson ?? string.Empty;
        }

        public DocumentEditorNativeFileActionKind Kind { get; }

        public string Html { get; }

        public string Markdown { get; }

        public string DocumentJson { get; }
    }
}
