// PhialeGis.Library.WinUi/Bridges/DslEditorTextSource.cs
using System;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using UniversalInput.Contracts;
using PhialeGis.Library.WinUi.Controls;

namespace PhialeGis.Library.WinUi.Interactions.Bridges
{
    /// <summary>Simple text/caret source projected from the PhialeDslEditor.</summary>
    internal sealed class DslEditorTextSource : IEditorTextSource, IDisposable
    {
        private readonly PhialeDslEditor _control;
        private readonly IEditorInteractive _editor;
        private int _caretOffset;
        private bool _disposed;

        public DslEditorTextSource(PhialeDslEditor control, IEditorInteractive editor)
        {
            if (control == null) throw new ArgumentNullException("control");
            if (editor == null) throw new ArgumentNullException("editor");
            _control = control;
            _editor = editor;
            _editor.CaretMovedUniversal += OnCaretMoved;
        }

        public string Text => _control?.Text ?? string.Empty;

        public int CaretOffset => _caretOffset;

        private void OnCaretMoved(object? sender, UniversalCaretMovedEventArgs e)
        {
            _caretOffset = e.Offset;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _editor.CaretMovedUniversal -= OnCaretMoved; } catch { }
        }
    }
}

