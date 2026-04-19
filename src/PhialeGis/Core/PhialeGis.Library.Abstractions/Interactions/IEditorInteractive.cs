// PhialeGis.Library.Core.Interactions/IEditorInteractive.cs
using UniversalInput.Contracts;
using System;


namespace PhialeGis.Library.Abstractions.Interactions
{
    /// <summary>
    /// Platform-agnostic editor interaction surface (WinRT-safe).
    /// Lives in Core so it can legally reference Universal*EventArgs.
    /// </summary>
    public interface IEditorInteractive
    {
        // Events from editor engine
        event EventHandler<UniversalTextChangedEventArgs> TextChangedUniversal;
        event EventHandler<UniversalSelectionChangedEventArgs> SelectionChangedUniversal;
        event EventHandler<UniversalCaretMovedEventArgs> CaretMovedUniversal;
        event EventHandler<UniversalDirtyChangedEventArgs> DirtyChangedUniversal;
        event EventHandler<UniversalCommandEventArgs> CommandUniversal;
        event EventHandler<UniversalSaveRequestedEventArgs> SaveRequestedUniversal;
        event EventHandler<UniversalLanguageChangedEventArgs> LanguageChangedUniversal;
        event EventHandler<UniversalThemeChangedEventArgs> ThemeChangedUniversal;
        event EventHandler<UniversalFindRequestedEventArgs> FindRequestedUniversal;
        event EventHandler<UniversalReplaceRequestedEventArgs> ReplaceRequestedUniversal;
        event EventHandler<UniversalLinkClickedEventArgs> LinkClickedUniversal;
        event EventHandler<UniversalDiagnosticsUpdatedEventArgs> DiagnosticsUpdatedUniversal;
        event EventHandler<UniversalHoverRequestedEventArgs> HoverRequestedUniversal;

        // Minimal state setters the core/host can call
        void SetText(string text);
        void SetLanguageId(string languageId);
        void SetReadOnly(bool isReadOnly);
    }
}

