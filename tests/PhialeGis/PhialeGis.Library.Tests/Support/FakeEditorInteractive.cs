using System;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;

namespace PhialeGis.Library.Tests.Support
{
    internal sealed class FakeEditorInteractive : IEditorInteractive
    {
        public event EventHandler<UniversalTextChangedEventArgs> TextChangedUniversal { add { } remove { } }
        public event EventHandler<UniversalSelectionChangedEventArgs> SelectionChangedUniversal { add { } remove { } }
        public event EventHandler<UniversalCaretMovedEventArgs> CaretMovedUniversal { add { } remove { } }
        public event EventHandler<UniversalDirtyChangedEventArgs> DirtyChangedUniversal { add { } remove { } }
        public event EventHandler<UniversalCommandEventArgs> CommandUniversal { add { } remove { } }
        public event EventHandler<UniversalSaveRequestedEventArgs> SaveRequestedUniversal { add { } remove { } }
        public event EventHandler<UniversalLanguageChangedEventArgs> LanguageChangedUniversal { add { } remove { } }
        public event EventHandler<UniversalThemeChangedEventArgs> ThemeChangedUniversal { add { } remove { } }
        public event EventHandler<UniversalFindRequestedEventArgs> FindRequestedUniversal { add { } remove { } }
        public event EventHandler<UniversalReplaceRequestedEventArgs> ReplaceRequestedUniversal { add { } remove { } }
        public event EventHandler<UniversalLinkClickedEventArgs> LinkClickedUniversal { add { } remove { } }
        public event EventHandler<UniversalDiagnosticsUpdatedEventArgs> DiagnosticsUpdatedUniversal { add { } remove { } }
        public event EventHandler<UniversalHoverRequestedEventArgs> HoverRequestedUniversal { add { } remove { } }

        public void SetText(string text) { }
        public void SetLanguageId(string languageId) { }
        public void SetReadOnly(bool isReadOnly) { }
    }
}


