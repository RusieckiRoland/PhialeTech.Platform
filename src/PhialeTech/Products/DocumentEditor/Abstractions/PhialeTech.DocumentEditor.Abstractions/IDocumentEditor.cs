using System;
using System.Threading.Tasks;

namespace PhialeTech.DocumentEditor.Abstractions
{
    public interface IDocumentEditor : IDisposable
    {
        DocumentEditorOptions Options { get; }

        bool IsInitialized { get; }

        bool IsReady { get; }

        DocumentEditorState State { get; }

        event EventHandler<DocumentEditorReadyStateChangedEventArgs> ReadyStateChanged;

        event EventHandler<DocumentEditorContentChangedEventArgs> ContentChanged;

        event EventHandler<DocumentEditorSelectionChangedEventArgs> SelectionChanged;

        event EventHandler<DocumentEditorErrorEventArgs> ErrorOccurred;

        Task InitializeAsync();

        Task SetHtmlAsync(string html);

        Task<string> GetHtmlAsync();

        Task SetMarkdownAsync(string markdown);

        Task<string> GetMarkdownAsync();

        Task SetDocumentJsonAsync(string documentJson);

        Task<string> GetDocumentJsonAsync();

        Task SetThemeAsync(string theme);

        Task SetLanguageAsync(string languageCode);

        Task SetOverlayModeAsync(DocumentEditorOverlayMode overlayMode);

        Task SetReadOnlyAsync(bool isReadOnly);

        Task SetToolbarAsync(DocumentEditorToolbarConfig toolbar);

        void FocusEditor();

        Task ExecuteCommandAsync(DocumentEditorCommand command, string value = null);
    }
}
