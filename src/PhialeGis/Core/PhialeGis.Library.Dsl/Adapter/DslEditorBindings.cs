using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Core.Interactions;
using PhialeGis.Library.DslEditor.Contracts;
using PhialeGis.Library.DslEditor.Runtime;
using System;
using System.Threading.Tasks;

namespace PhialeGis.Library.Dsl.Adapter
{
    /// <summary>
    /// Glue layer between GisInteractionManager and an IDslEngine.
    /// Provides one-shot binding (BindDsl) and fine-grained bindings.
    /// </summary>
    public static class DslEditorBindings
    {
        /// <summary>
        /// One-shot wiring for all DSL providers (completions, exec, validate, semantics).
        /// </summary>
        public static void BindDsl(GisInteractionManager core, IDslEngine engine)
        {
            var dslContextProvider = core.DslContextProvider;
            EnsureArgs(core, engine);
            BindCompletions(core, engine,dslContextProvider);
            BindSemantics(core, engine, dslContextProvider);
            BindExec(core, engine, dslContextProvider);
            BindValidate(core, engine, dslContextProvider);
        }

        /// <summary>
        /// Binds completion provider (IDslEngine is the single source of truth).
        /// </summary>
        public static void BindCompletions(GisInteractionManager core, IDslEngine engine, IDslContextProvider ctxProvider )
        {
            EnsureArgs(core, engine);
            var dslMgr = GetDslManager(core);

            dslMgr.SetCompletionProvider((code, caret, editor) =>
            {
                var dto = engine.GetCompletions(code ?? string.Empty, caret, editor, ctxProvider)
                         ?? new DslCompletionListDto { Items = Array.Empty<DslCompletionItemDto>(), IsIncomplete = false };

                var src = dto.Items ?? Array.Empty<DslCompletionItemDto>();
                var items = new DslCompletionItemDto[src.Length];

                for (int i = 0; i < src.Length; i++)
                {
                    var it = src[i] ?? new DslCompletionItemDto();
                    items[i] = new DslCompletionItemDto
                    {
                        // Keep label non-null
                        Label = it.Label ?? string.Empty,
                        // Default InsertText to label if missing
                        InsertText = string.IsNullOrEmpty(it.InsertText) ? (it.Label ?? string.Empty) : it.InsertText,
                        Kind = it.Kind
                    };
                }

                return Task.FromResult(new DslCompletionListDto
                {
                    Items = items,
                    IsIncomplete = dto.IsIncomplete
                });
            });
        }

        /// <summary>
        /// Binds async execution. Ambient (DslAmbient) context is pushed by manager's factory.
        /// </summary>
        public static void BindExec(GisInteractionManager core, IDslEngine engine, IDslContextProvider ctxProvider)
        {
            EnsureArgs(core, engine);
            var dslMgr = GetDslManager(core);

            Task<DslResultDto> exec(string code, DslCommandEnvelope env)
            {
                // Keep the current target in the interaction manager for this call.
                core.CurrentDslTarget = env?.Target;
                var target = env?.Target;
                var source = env.Editor;

                return Task.Run(() =>
                {
                    var dto = engine.Execute(code ?? string.Empty, target, source, ctxProvider);
                    return new DslResultDto
                    {
                        Success = dto.Success,
                        Output = dto.Output,
                        Error = dto.Error
                    };
                });
            }

            dslMgr.SetAsyncExec(exec);
        }

        /// <summary>
        /// Binds async validation and maps diagnostics to runtime DTOs.
        /// </summary>
        public static void BindValidate(GisInteractionManager core, IDslEngine engine, IDslContextProvider ctxProvider)
        {
            EnsureArgs(core, engine);
            var dslMgr = GetDslManager(core);

            Task<DslValidationResultDto> validate(string code, IEditorInteractive editor)
            {
                return Task.Run(() =>
                {
                    var dto = engine.Validate(code ?? string.Empty,editor,ctxProvider);
                    var diags = new DslDiagnosticDto[dto.Diagnostics.Length];

                    for (int i = 0; i < dto.Diagnostics.Length; i++)
                    {
                        var d = dto.Diagnostics[i];
                        diags[i] = new DslDiagnosticDto
                        {
                            Line = d.Line,
                            Column = d.Column,
                            Message = d.Message,
                            Length = d.Length,              
                            Severity = d.Severity
                        };
                    }

                    return new DslValidationResultDto
                    {
                        IsValid = dto.IsValid,
                        Diagnostics = diags
                    };
                });
            }

            dslMgr.SetAsyncValidate(validate);
        }

        /// <summary>
        /// Binds semantic legend & tokens providers.
        /// </summary>
        public static void BindSemantics(GisInteractionManager core, IDslEngine engine, IDslContextProvider ctxProvider)
        {
            EnsureArgs(core, engine);
            var dslMgr = GetDslManager(core);

            // Legend provider
            dslMgr.SetSemanticLegendProvider(() =>
                Task.FromResult(engine.GetSemanticLegend()));

            // Tokens provider
            dslMgr.SetSemanticTokensProvider((code , _editor) =>
                Task.FromResult(engine.GetSemanticTokens(code ?? string.Empty)));
        }

        // ----------------------
        // Helpers
        // ----------------------

        /// <summary>
        /// Validates arguments consistently.
        /// </summary>
        private static void EnsureArgs(GisInteractionManager core, IDslEngine engine)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            if (engine == null) throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Extracts the concrete DslInteractionManager from the core.Editors slot.
        /// </summary>
        private static DslInteractionManager GetDslManager(GisInteractionManager core)
        {
            return core.Editors as DslInteractionManager
                   ?? throw new InvalidOperationException("Core.Editors is not a DslInteractionManager.");
        }
    }
}
