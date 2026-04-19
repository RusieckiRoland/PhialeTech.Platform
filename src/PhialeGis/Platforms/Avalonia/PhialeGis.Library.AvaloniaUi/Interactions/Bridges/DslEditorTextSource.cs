// DslEditorTextSource.cs (Avalonia)
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using UniversalInput.Contracts;
using PhialeGis.Library.AvaloniaUi.Controls;
using System;

namespace PhialeGis.Library.AvaloniaUi.Interactions.Bridges
{
    internal sealed class DslEditorTextSource : IEditorTextSource, IDisposable
    {
        private readonly PhialeDslEditor _control;
        private readonly IEditorInteractive _editor;
        private int _caretOffset;
        private bool _disposed;

        public DslEditorTextSource(PhialeDslEditor control, IEditorInteractive editor)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _editor.CaretMovedUniversal += OnCaretMoved;
        }

        public string Text => _control.Text ?? string.Empty;

        public int CaretOffset => _caretOffset;

        private void OnCaretMoved(object? sender, UniversalCaretMovedEventArgs e)
        {
            _caretOffset = e.Offset;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                _editor.CaretMovedUniversal -= OnCaretMoved;
            }
            catch
            {
                // best effort
            }
        }
    }
}

