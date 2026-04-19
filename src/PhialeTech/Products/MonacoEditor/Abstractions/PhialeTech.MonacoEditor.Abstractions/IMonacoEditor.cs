using System;
using System.Threading.Tasks;

namespace PhialeTech.MonacoEditor.Abstractions
{
    public interface IMonacoEditor : IDisposable
    {
        MonacoEditorOptions Options { get; }

        bool IsInitialized { get; }

        bool IsReady { get; }

        string Value { get; }

        string Language { get; }

        string Theme { get; }

        event EventHandler<MonacoEditorReadyStateChangedEventArgs> ReadyStateChanged;

        event EventHandler<MonacoEditorContentChangedEventArgs> ContentChanged;

        event EventHandler<MonacoEditorErrorEventArgs> ErrorOccurred;

        Task InitializeAsync();

        Task SetValueAsync(string value);

        Task<string> GetValueAsync();

        Task SetLanguageAsync(string language);

        Task SetThemeAsync(string theme);

        void FocusEditor();
    }
}
