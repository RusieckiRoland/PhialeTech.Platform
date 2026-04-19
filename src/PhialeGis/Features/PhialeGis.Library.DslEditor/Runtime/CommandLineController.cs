// PhialeGis.Library.DslEditor/Runtime/CommandLineController.cs
using System;
using System.Threading.Tasks;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.DslEditor.Contracts;

namespace PhialeGis.Library.DslEditor.Runtime
{
    /// <summary>
    /// Command-mode behavior: handles submit, history navigation and completions.
    /// UI must call the methods on keystrokes (Enter/Up/Down/Tab/Ctrl+Space).
    /// </summary>
    public sealed class CommandLineController
    {
        private readonly IDslEditorManager _dsl;
        private readonly IEditorInteractive _editor;
        private readonly IEditorTextSource _textSource;
        private readonly object _target; // viewport adapter or null
        private readonly CommandHistory _history;
        private bool _busy;

        public CommandLineController(
            IDslEditorManager dsl,
            IEditorInteractive editor,
            IEditorTextSource textSource,
            object targetViewportAdapterOrNull,
            int historyCapacity = 200)
        {
            if (dsl == null) throw new ArgumentNullException("dsl");
            if (editor == null) throw new ArgumentNullException("editor");
            if (textSource == null) throw new ArgumentNullException("textSource");

            _dsl = dsl;
            _editor = editor;
            _textSource = textSource;
            _target = targetViewportAdapterOrNull;
            _history = new CommandHistory(historyCapacity);
        }

        /// <summary>
        /// Submits current line/buffer as a command: pushes to history, clears input,
        /// executes via DSL engine and raises echo/result events.
        /// </summary>
        public async Task SubmitAsync()
        {
            if (_busy) return;

            var code = (_textSource.Text ?? string.Empty).Trim();
            if (code.Length == 0) return;

            _busy = true;
            try
            {
                _history.Push(code);
                OnEcho(new CommandEchoEventArgs(code));

                // Clear input immediately for CLI feel
                SafeSetEditorText(string.Empty);

                var env = new DslCommandEnvelope(_editor, _target, "enter", false, false);
                var result = await _dsl.ExecuteAsync(code, env).ConfigureAwait(false);

                OnExecuted(new CommandExecutedEventArgs(result != null && result.Success,
                                                       result != null ? result.Output : string.Empty,
                                                       result != null ? result.Error : string.Empty));
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>
        /// Saves current draft and navigates to previous history item.
        /// Returns text to be put into the input (caller sets it).
        /// </summary>
        public string HistoryPrev()
        {
            _history.SaveDraft(_textSource.Text ?? string.Empty);
            var h = _history.Prev();
            return h ?? (_textSource.Text ?? string.Empty);
        }

        /// <summary>
        /// Navigates to next history item (or draft when leaving history).
        /// Returns text to be put into the input (caller sets it).
        /// </summary>
        public string HistoryNext()
        {
            var h = _history.Next();
            return h ?? string.Empty;
        }

        /// <summary>
        /// Requests completions for current caret position.
        /// </summary>
        public async Task<DslCompletionListDto> RequestCompletionsAsync()
        {
            var code = _textSource.Text ?? string.Empty;
            var caret = _textSource.CaretOffset;
            var list = await _dsl.GetCompletionsAsync(code, caret, _editor).ConfigureAwait(false);
            return list ?? new DslCompletionListDto { Items = new DslCompletionItemDto[0], IsIncomplete = false };
        }

        /// <summary>
        /// Clears history and input (does not affect output pane).
        /// </summary>
        public void ClearInputAndHistory()
        {
            _history.Clear();
            SafeSetEditorText(string.Empty);
        }

        /// <summary>
        /// Sets the placeholder via UI if supported. No-op by default.
        /// </summary>
        public void SetPlaceholder(string placeholder)
        {
            // Intentionally empty: leave to UI host (JS/Control) if supported.
            // Kept here for future expansion (e.g., IEditorInteractive.SetPlaceholder).
        }

        // ----------------- Events exposed to host (UI/ViewModel) -----------------

        /// <summary>Fired when a command is echoed (before execution).</summary>
        public event EventHandler<CommandEchoEventArgs> Echo;

        /// <summary>Fired when command execution finishes.</summary>
        public event EventHandler<CommandExecutedEventArgs> Executed;

        private void OnEcho(CommandEchoEventArgs e)
        {
            var h = Echo;
            if (h != null) h(this, e);
        }

        private void OnExecuted(CommandExecutedEventArgs e)
        {
            var h = Executed;
            if (h != null) h(this, e);
        }

        // ----------------- Helpers -----------------

        private void SafeSetEditorText(string text)
        {
            try { _editor.SetText(text ?? string.Empty); } catch { /* ignore UI failures */ }
        }
    }
}
