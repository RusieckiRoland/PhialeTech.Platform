using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.Abstractions.Interactions;
using UniversalInput.Contracts;
using PhialeGis.Library.UwpUi.Controls;
using System;

namespace PhialeGis.Library.UwpUi.Interactions.Bridges
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

        public string Text { get { return _control?.Text ?? string.Empty; } }

        public int CaretOffset { get { return _caretOffset; } }

        private void OnCaretMoved(object sender, UniversalCaretMovedEventArgs e)
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

