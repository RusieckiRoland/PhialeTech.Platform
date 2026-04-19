using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhialeGis.Library.Abstractions.Actions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Interactions.Editors;
using UniversalInput.Contracts;
using PhialeGis.Library.DslEditor.Contracts;

namespace PhialeGis.Library.DslEditor.Runtime
{
    /// <summary>
    /// Manages interactive editors and forwards their requests to the DSL engine.
    /// Single source of truth lives in the engine; no token/rule duplication here.
    /// </summary>
    public sealed class DslInteractionManager : IDslEditorManager
    {
        private static readonly Func<string, DslCommandEnvelope, Task<DslResultDto>> _defaultExecuteAsync =
            (code, envelope) => Task.FromResult(new DslResultDto
            {
                Success = false,
                Output = string.Empty,
                Error = string.Empty
            });

        private static readonly Func<string, IEditorInteractive, Task<DslValidationResultDto>> _defaultValidateAsync =
            (code, editor) => Task.FromResult(new DslValidationResultDto
            {
                IsValid = true,
                Diagnostics = Array.Empty<DslDiagnosticDto>()
            });

        private static readonly Func<Task<DslSemanticLegendDto>> _defaultLegendAsync =
            () => Task.FromResult(new DslSemanticLegendDto());

        private static readonly Func<string, IEditorInteractive, Task<DslSemanticTokensDto>> _defaultTokensAsync =
            (code, editor) => Task.FromResult(new DslSemanticTokensDto());

        private static readonly Func<string, int, IEditorInteractive, Task<DslCompletionListDto>> _defaultCompletionsAsync =
            (text, caret, editor) => Task.FromResult(new DslCompletionListDto
            {
                Items = Array.Empty<DslCompletionItemDto>(),
                IsIncomplete = false
            });

        // ----------------------- DSL engine providers -----------------------
        private Func<string, DslCommandEnvelope, Task<DslResultDto>> _executeAsync = _defaultExecuteAsync;

        private Func<string, IEditorInteractive, Task<DslValidationResultDto>> _validateAsync = _defaultValidateAsync;

        // BYŁO: private Func<Task<DslSemanticLegend>> _legendAsync;
        private Func<Task<DslSemanticLegendDto>> _legendAsync = _defaultLegendAsync;

        // BYŁO: private Func<string, Task<DslSemanticTokens>> _tokensAsync;
        private Func<string, IEditorInteractive, Task<DslSemanticTokensDto>> _tokensAsync = _defaultTokensAsync;

        public void SetSemanticLegendProvider(Func<Task<DslSemanticLegendDto>> f) => _legendAsync = f;

        public void SetSemanticTokensProvider(Func<string, IEditorInteractive, Task<DslSemanticTokensDto>> f) => _tokensAsync = f;

        public Task<DslSemanticLegendDto> GetSemanticLegendAsync()
    => _legendAsync != null ? _legendAsync() : Task.FromResult(new DslSemanticLegendDto());

        public Task<DslSemanticTokensDto> GetSemanticTokensAsync(string code, IEditorInteractive _editor)
            => _tokensAsync != null ? _tokensAsync(code ?? string.Empty,_editor) : Task.FromResult(new DslSemanticTokensDto());

        // Completions are plugged later to avoid ref cycles.
        private Func<string, int, IEditorInteractive, Task<DslCompletionListDto>> _completionsAsync = _defaultCompletionsAsync;

        

        /// <summary>
        /// Fired whenever a new prompt is available for the active editor.
        /// DslEditorConnector listens to this to forward the prompt to the control.
        /// </summary>
        public event EventHandler<DslPromptDto> PromptChanged;
        /// <summary>
        /// Fired whenever a new prompt is available for a specific editor.
        /// </summary>
        public event EventHandler<EditorPromptChangedEventArgs> PromptChangedForEditor;

        /// <summary>
        /// Minimal constructor – inject Execute and Validate providers.
        /// Completions can be added later via <see cref="SetCompletionProvider"/>.
        /// </summary>
        ///

        public DslInteractionManager(
             )
        {
            
        }

        /// <summary>
        /// Backward-compatible constructor – also accepts a completions provider.
        /// </summary>
        public DslInteractionManager( Func<string, int, IEditorInteractive, Task<DslCompletionListDto>> completionsAsync)
            
        {
            if (completionsAsync == null) throw new ArgumentNullException(nameof(completionsAsync));
            _completionsAsync = completionsAsync;
        }

        /// <summary>Injects the asynchronous completion provider.</summary>
        public void SetCompletionProvider(Func<string, int, IEditorInteractive, Task<DslCompletionListDto>> provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _completionsAsync = provider;
        }

        // ----------------------------- Editor registry -----------------------------
        private readonly HashSet<IEditorInteractive> _editors = new HashSet<IEditorInteractive>();

        /// <summary>The editor that is currently active (if any).</summary>
        public IEditorInteractive ActiveEditor { get; private set; }

        /// <summary>Registers an editor and wires event handlers.</summary>
        public void RegisterEditor(object editorObj)
        {
            if (editorObj == null) throw new ArgumentNullException(nameof(editorObj));

            var editor = editorObj as IEditorInteractive;
            if (editor == null)
                throw new ArgumentException("Editor must implement IEditorInteractive.", nameof(editorObj));

            if (_editors.Add(editor))
            {
                // Activate this editor on any user interaction — important in multi-editor setups.
                editor.CommandUniversal += OnEditorCommand;
                editor.CaretMovedUniversal += OnEditorCaretMoved;
                editor.SelectionChangedUniversal += OnEditorSelectionChanged;
                editor.TextChangedUniversal += OnEditorTextChanged;

                if (ActiveEditor == null) ActiveEditor = editor;
            }
        }

        /// <summary>Unregisters an editor and unwires handlers.</summary>
        public void UnregisterEditor(object editorObj)
        {
            if (editorObj == null) return;

            var editor = editorObj as IEditorInteractive;
            if (editor == null) return;

            if (_editors.Remove(editor))
            {
                editor.CommandUniversal -= OnEditorCommand;
                editor.CaretMovedUniversal -= OnEditorCaretMoved;
                editor.SelectionChangedUniversal -= OnEditorSelectionChanged;
                editor.TextChangedUniversal -= OnEditorTextChanged;

                if (ActiveEditor == editor) ActiveEditor = null;
            }
        }

        /// <summary>Sets the active editor (must be already registered).</summary>
        public void SetActiveEditor(IEditorInteractive editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (_editors.Contains(editor)) ActiveEditor = editor;
        }

        // --------------------------------- Events --------------------------------
        /// <summary>Raised when a command envelope arrives from an editor (e.g., Enter).</summary>
        public event EventHandler<DslCommandEnvelope> CommandReceived;


        // ------------------------------- Event handlers ------------------------------
        private void OnEditorCommand(object sender, object payload)
        {
            // Mark sender as active to disambiguate in multi-editor scenarios.
            var ed = sender as IEditorInteractive;
            if (ed != null) ActiveEditor = ed;

            var handler = CommandReceived;
            if (handler == null) return;

            var env = payload as DslCommandEnvelope;

            // Minimal fallback – if someone sent only a string id, create an envelope.
            if (env == null)
            {
                var cmdId = payload as string;
                if (!string.IsNullOrEmpty(cmdId))
                {
                    env = new DslCommandEnvelope(ActiveEditor, null, cmdId, false, false);
                }
            }

            if (env != null)
                handler(this, env);
        }

        private void OnEditorCaretMoved(object sender, UniversalCaretMovedEventArgs e)
        {
            var ed = sender as IEditorInteractive;
            if (ed != null && !ReferenceEquals(ActiveEditor, ed))
                ActiveEditor = ed;
        }

        private void OnEditorSelectionChanged(object sender, UniversalSelectionChangedEventArgs e)
        {
            var ed = sender as IEditorInteractive;
            if (ed != null && !ReferenceEquals(ActiveEditor, ed))
                ActiveEditor = ed;
        }

        private void OnEditorTextChanged(object sender, UniversalTextChangedEventArgs e)
        {
            var ed = sender as IEditorInteractive;
            if (ed != null && !ReferenceEquals(ActiveEditor, ed))
                ActiveEditor = ed;
        }

        // --------------------------- DSL operations (public) ---------------------------
        public Task<DslResultDto> ExecuteAsync(string code, DslCommandEnvelope envelope)
        {
            return (_executeAsync ?? _defaultExecuteAsync)(code ?? string.Empty, envelope);
        }

        public Task<DslValidationResultDto> ValidateAsync(string code, IEditorInteractive editor)
        {
            return (_validateAsync ?? _defaultValidateAsync)(code ?? string.Empty, editor);
        }

        public Task<DslCompletionListDto> GetCompletionsAsync(string text, int caret, IEditorInteractive editor)
        {
            var f = _completionsAsync ?? _defaultCompletionsAsync;
            return f(text ?? string.Empty, caret, editor);
        }

        // ------------------------ Convenience triggers for UI ------------------------
        //public async Task TriggerValidationForActiveEditorAsync(IEditorTextSource textSource)
        //{
        //    if (textSource == null) return;
        //    var result = await ValidateAsync(textSource.Text).ConfigureAwait(false);
        //    var h = ValidationReady; if (h != null) h(this, result);
        //}

        //public async Task TriggerCompletionsForActiveEditorAsync(IEditorTextSource textSource)
        //{
        //    if (textSource == null) return;
        //    var list = await GetCompletionsAsync(textSource.Text, textSource.CaretOffset).ConfigureAwait(false);
        //    var h = CompletionsReady; if (h != null) h(this, list);
        //}

        /// <summary>
        /// Broadcast the prompt to listeners (e.g., connectors bound to the active editor).
        /// </summary>
        public void PushPromptForActiveEditor(DslPromptDto dto)
        {
            try
            {
                PromptChanged?.Invoke(this, dto);
                if (ActiveEditor != null)
                {
                    _lastPromptByEditor[ActiveEditor] = dto;
                    PromptChangedForEditor?.Invoke(this, new EditorPromptChangedEventArgs(ActiveEditor, dto));
                }
            }
            catch
            {
                // Best-effort: do not allow UI helpers to bring the manager down.
            }
        }

        public void PushPromptForEditor(IEditorInteractive editor, DslPromptDto dto)
        {
            if (editor == null) return;
            try
            {
                _lastPromptByEditor[editor] = dto;
                PromptChangedForEditor?.Invoke(this, new EditorPromptChangedEventArgs(editor, dto));
                if (ReferenceEquals(editor, ActiveEditor))
                    PromptChanged?.Invoke(this, dto);
            }
            catch
            {
                // Best-effort
            }
        }

        // --------------------------- Prompt cache ---------------------------
        private readonly Dictionary<IEditorInteractive, DslPromptDto> _lastPromptByEditor =
            new Dictionary<IEditorInteractive, DslPromptDto>();

        public bool TryGetLastPrompt(IEditorInteractive editor, out DslPromptDto dto)
            => _lastPromptByEditor.TryGetValue(editor, out dto);

        public void SetAsyncExec(Func<string, DslCommandEnvelope, Task<DslResultDto>> exec)
        {
            if (exec == null) throw new ArgumentNullException(nameof(exec));
            _executeAsync = exec;
        }

        public void SetAsyncValidate(Func<string, IEditorInteractive, Task<DslValidationResultDto>> validate)
        {
            if (validate == null) throw new ArgumentNullException(nameof(validate));
            _validateAsync = validate;
        }
    }
}

