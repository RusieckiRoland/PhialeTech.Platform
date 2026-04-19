using System;

namespace PhialeGis.Library.Abstractions.Interactions.Editors
{
    // Minimal text source for editors. WinRT-safe.
    public interface IEditorTextSource
    {
        string Text { get; }
        int CaretOffset { get; }
    }
}
