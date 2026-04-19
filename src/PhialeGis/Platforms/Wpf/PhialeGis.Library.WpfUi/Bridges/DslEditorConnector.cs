// PhialeGis.Library.WpfUi.Interactions.Bridges/DslEditorConnector.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.DslEditor.Contracts;
using PhialeGis.Library.DslEditor.Interop;
using PhialeGis.Library.DslEditor.Runtime;

namespace PhialeGis.Library.WpfUi.Interactions.Bridges
{
    /// <summary>
    /// Bridges the WebView-based editor with the DSL manager.
    /// - Registers the editor with the manager;
    /// - Triggers completions on demand;
    /// - Debounces semantic token refreshes;
    /// - Never touches DependencyProperties from background threads.
    /// </summary>
    internal sealed class DslEditorConnector : IDisposable
    {
        private readonly IDslEditorManager _editors;      // DSL editor manager
        private readonly IEditorInteractive _editor;      // same adapter instance
        private readonly IEditorTextSource _textSource;   // same adapter instance
        private readonly int _debounceMs;

        private readonly object _gate = new object();
        private Timer _debounceTimer;
        private bool _disposed;

        // Cached snapshot of editor state captured on UI thread via OnTextChanged()/caret move.
        private volatile string _lastText = string.Empty;
        private volatile int _lastCaret = 0;

        public event EventHandler<DslSemanticLegendDto> SemanticLegendAvailable;
        public event EventHandler<DslSemanticTokensDto> SemanticTokensAvailable;
        public event EventHandler<DslValidationResultDto> ValidationAvailable;

        internal event EventHandler<object> CompletionsAvailable;

        /// <summary>
        /// Raised when the DSL manager pushes a prompt for the active editor.
        /// The control subscribes to this event to call SetFSMStateAsync().
        /// </summary>
        internal event EventHandler<DslPromptDto> PromptAvailable;

        public DslEditorConnector(
            IDslEditorManager editors,
            IEditorInteractive editor,
            IEditorTextSource textSource,
            int debounceMs = 250)
        {
            _editors = editors ?? throw new ArgumentNullException(nameof(editors));
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _textSource = textSource ?? throw new ArgumentNullException(nameof(textSource));
            _debounceMs = debounceMs > 0 ? debounceMs : 1;

            // Always register the editor with the DSL manager
            _editors.RegisterEditor(_editor);

            _editors.PromptChangedForEditor += OnPromptChangedForEditor;

            // Initialize cache (we are on UI thread when constructed from the control)
            _lastText = _textSource.Text ?? string.Empty;
            _lastCaret = _textSource.CaretOffset;
        }

        /// <summary>
        /// When action prompt is changing
        /// </summary>
        private void OnPromptChangedForEditor(object sender, EditorPromptChangedEventArgs e)
        {
            try
            {
                if (!ReferenceEquals(e.Editor, _editor)) return;
                // Forward to the control. The control stays responsible for marshaling to UI thread if needed.
                PromptAvailable?.Invoke(this, e.Prompt);
            }
            catch
            {
                // Best-effort
            }
        }

        /// <summary>
        /// Called by the control on each text change (UI thread).
        /// We cache text/caret and schedule a debounced semantic refresh on a timer thread.
        /// </summary>
        internal void OnTextChanged()
        {
            if (_disposed) return;

            // Cache on UI thread to avoid cross-thread DP access later
            _lastText = _textSource.Text ?? string.Empty;
            _lastCaret = _textSource.CaretOffset;

            lock (_gate)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(async _ => await DebouncedTickAsync().ConfigureAwait(false),
                                           null, _debounceMs, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Triggered by Ctrl+Space (UI thread). Uses cached text/caret to avoid cross-thread DP access.
        /// </summary>
        internal async void RequestCompletions()
        {
            if (_disposed || _editors == null) return;

            try
            {
                // Mark this editor as active (optional, helps multi-editor setups)
                (_editors as DslInteractionManager)?.SetActiveEditor(_editor);

                // Always read the current snapshot from the live text source (no cache here)
                var text = _textSource?.Text ?? string.Empty;
                var caret = _textSource?.CaretOffset ?? text.Length;

                // Normalize caret to a valid range
                if (caret < 0 || caret > text.Length) caret = text.Length;

                // Ask the DSL layer for completions based on the fresh snapshot
                var list = await _editors.GetCompletionsAsync(text, caret, _editor).ConfigureAwait(false);

                // Push results to the UI
                CompletionsAvailable?.Invoke(this, list);
            }
            catch
            {
                // Best-effort: keep the UI responsive even if completions fail
            }
        }

        /// <summary>
        /// Forces legend + tokens push (used after WebView navigation completes).
        /// </summary>
        public void RequestSemanticRefreshNow()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var legend = await _editors.GetSemanticLegendAsync().ConfigureAwait(false);
                    SemanticLegendAvailable?.Invoke(this, legend);

                    var code = _lastText ?? string.Empty;
                    var toks = await _editors.GetSemanticTokensAsync(code, _editor).ConfigureAwait(false);
                    SemanticTokensAvailable?.Invoke(this, toks);

                    var val = await _editors.ValidateAsync(code, _editor).ConfigureAwait(false);
                    ValidationAvailable?.Invoke(this, val ?? new DslValidationResultDto());
                }
                catch
                {
                    // best-effort
                }
            });
        }

        /// <summary>
        /// Debounced semantic token refresh using cached text snapshot.
        /// </summary>
        private async Task DebouncedTickAsync()
        {
            try
            {
                var code = _lastText ?? string.Empty;
                var caret = _lastCaret;

                // 1) Semantics
                var toks = await _editors.GetSemanticTokensAsync(code, _editor).ConfigureAwait(false);
                SemanticTokensAvailable?.Invoke(this, toks);

                // 2) Heuristic: typing a token -> hold validation and CLEAR previous diagnostics
                bool IsWordChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-';

                bool caretInsideToken =
                    (caret > 0 && caret <= code.Length && IsWordChar(code[caret - 1])) ||
                    (caret < code.Length && IsWordChar(code[caret]));

                if (caretInsideToken)
                {
                    // Clear old underlines so a previous diagnostic doesn't "stick"
                    ValidationAvailable?.Invoke(this, new DslValidationResultDto
                    {
                        IsValid = true,
                        Diagnostics = Array.Empty<DslDiagnosticDto>()
                    });
                    return;
                }

                // 3) Validation (single path: manager -> bindings -> engine)
                var validation = await _editors.ValidateAsync(code, _editor).ConfigureAwait(false);
                ValidationAvailable?.Invoke(this, validation ?? new DslValidationResultDto());
            }
            catch
            {
                // silent
            }
        }

        /// <summary>
        /// Execute command immediately (Enter / Ctrl+Enter).
        /// </summary>
        internal async Task ExecuteNowAsync(string code, bool isScript)
        {
            if (_disposed) return;

            try
            {
                (_editors as DslInteractionManager)?.SetActiveEditor(_editor);

                object target = null;
                if (_editor is IEditorViewportLink link)
                    target = link.GetAttachedViewportAdapterOrNull();

                var env = new DslCommandEnvelope(_editor, target, "enter", isScript, false);
                await _editors.ExecuteAsync(code ?? string.Empty, env).ConfigureAwait(false);
            }
            catch
            {
                // keep UI responsive
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_gate)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }

            try
            {
                _editors.PromptChangedForEditor -= OnPromptChangedForEditor;
                _editors.UnregisterEditor(_editor);
            }
            catch
            {
                /* idempotent */
            }
        }
    }
}
