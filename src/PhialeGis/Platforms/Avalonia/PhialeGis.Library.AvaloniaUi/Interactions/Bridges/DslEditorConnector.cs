// DslEditorConnector.cs (Avalonia)
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using PhialeGis.Library.DslEditor.Contracts;
using PhialeGis.Library.DslEditor.Interop;
using PhialeGis.Library.DslEditor.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGis.Library.AvaloniaUi.Interactions.Bridges
{
    internal sealed class DslEditorConnector : IDisposable
    {
        private readonly IDslEditorManager _editors;
        private readonly IEditorInteractive _editor;
        private readonly IEditorTextSource _textSource;
        private readonly int _debounceMs;

        private readonly object _gate = new object();
        private Timer? _debounceTimer;
        private bool _disposed;

        private volatile string _lastText = string.Empty;
        private volatile int _lastCaret = 0;

        public event EventHandler<DslSemanticLegendDto>? SemanticLegendAvailable;
        public event EventHandler<DslSemanticTokensDto>? SemanticTokensAvailable;
        public event EventHandler<DslValidationResultDto>? ValidationAvailable;

        internal event EventHandler<object?>? CompletionsAvailable;
        internal event EventHandler<DslPromptDto>? PromptAvailable;

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

            _editors.RegisterEditor(_editor);
            _editors.PromptChangedForEditor += OnPromptChangedForEditor;

            _lastText = _textSource.Text ?? string.Empty;
            _lastCaret = _textSource.CaretOffset;
        }

        private void OnPromptChangedForEditor(object? sender, EditorPromptChangedEventArgs e)
        {
            try
            {
                if (!ReferenceEquals(e.Editor, _editor)) return;
                PromptAvailable?.Invoke(this, e.Prompt);
            }
            catch
            {
                // Best-effort
            }
        }

        internal void OnTextChanged()
        {
            if (_disposed)
                return;

            _lastText = _textSource.Text ?? string.Empty;
            _lastCaret = _textSource.CaretOffset;

            lock (_gate)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(
                    async _ => await DebouncedTickAsync().ConfigureAwait(false),
                    null,
                    _debounceMs,
                    Timeout.Infinite);
            }
        }

        internal async void RequestCompletions()
        {
            if (_disposed)
                return;

            try
            {
                (_editors as DslInteractionManager)?.SetActiveEditor(_editor);

                var text = _textSource.Text ?? string.Empty;
                var caret = _textSource.CaretOffset;
                if (caret < 0 || caret > text.Length)
                    caret = text.Length;

                var list = await _editors.GetCompletionsAsync(text, caret, _editor).ConfigureAwait(false);
                CompletionsAvailable?.Invoke(this, list);
            }
            catch
            {
                // Best-effort
            }
        }

        public void RequestSemanticRefreshNow()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var legend = await _editors.GetSemanticLegendAsync().ConfigureAwait(false);
                    SemanticLegendAvailable?.Invoke(this, legend);

                    var code = _lastText ?? string.Empty;
                    var tokens = await _editors.GetSemanticTokensAsync(code, _editor).ConfigureAwait(false);
                    SemanticTokensAvailable?.Invoke(this, tokens);

                    var validation = await _editors.ValidateAsync(code, _editor).ConfigureAwait(false);
                    ValidationAvailable?.Invoke(this, validation ?? new DslValidationResultDto());
                }
                catch
                {
                    // Best-effort
                }
            });
        }

        private async Task DebouncedTickAsync()
        {
            try
            {
                var code = _lastText ?? string.Empty;

                var tokens = await _editors.GetSemanticTokensAsync(code, _editor).ConfigureAwait(false);
                SemanticTokensAvailable?.Invoke(this, tokens);

                var validation = await _editors.ValidateAsync(code, _editor).ConfigureAwait(false);
                ValidationAvailable?.Invoke(this, validation ?? new DslValidationResultDto());
            }
            catch
            {
                // Best-effort
            }
        }

        internal async Task ExecuteNowAsync(string code, bool isScript)
        {
            if (_disposed)
                return;

            try
            {
                (_editors as DslInteractionManager)?.SetActiveEditor(_editor);

                object? target = null;
                if (_editor is IEditorViewportLink link)
                    target = link.GetAttachedViewportAdapterOrNull();

                var env = new DslCommandEnvelope(_editor, target, "enter", isScript, false);
                await _editors.ExecuteAsync(code ?? string.Empty, env).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

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
                // Idempotent cleanup
            }
        }
    }
}
