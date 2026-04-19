using Antlr4.Runtime;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Api;
using PhialeGis.Library.Dsl.Features;
using PhialeGis.Library.DslEditor.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhialeGis.Library.Dsl.Adapter
{
    /// <summary>
    /// Thin adapter that implements IDslEngine on top of the context-bound Dsl.Api.DslEngine.
    /// It resolves (viewport, graphics) per call and delegates to the real engine.
    /// </summary>
    public sealed class PhGisDslEngineAdapter : IDslEngine
    {
        private readonly PhGis _gis;
        private readonly Func<string, Tuple<IViewport, IGraphicsFacade>> _resolve;

        public Action<DslEngine> AttachManager;

        public PhGisDslEngineAdapter(PhGis gis, Func<string, Tuple<IViewport, IGraphicsFacade>> resolve)
        {
            if (gis == null) throw new ArgumentNullException("gis");
            if (resolve == null) throw new ArgumentNullException("resolve");
            _gis = gis;
            _resolve = resolve;
        }

        public DslResultDto Execute(string code, object target, IEditorInteractive source, IDslContextProvider ctxProvider)
        {
            var targetKey = target != null ? target.ToString() ?? string.Empty : string.Empty;
            var ctx = _resolve(targetKey);
            var engine = new DslEngine(_gis, ctx.Item1, ctx.Item2);

            // UNIKAJ oczekiwania na UI: wyłącz przechwytywanie kontekstu
            var task = engine.ParseAndExecuteAsync(code ?? string.Empty,target, source, ctxProvider);
            AttachManager?.Invoke(engine);

            var r = task.ConfigureAwait(false).GetAwaiter().GetResult();

            return new DslResultDto
            {
                Success = r.Executed,
                Output = string.Empty,
                Error = (r.Diagnostics != null && r.Diagnostics.Count > 0) ? r.Diagnostics[0].Message : string.Empty
            };
        }

        public DslValidationResultDto Validate(string code, IEditorInteractive editor,IDslContextProvider ctxProvider)
        {
            var validator = new PhialeGis.Library.Dsl.Validation.DslDiagnosticValidator();
            var list = validator.Validate(code ?? string.Empty,editor, ctxProvider);

            var dtos = new List<DslDiagnosticDto>(list.Count);
            foreach (var d in list)
            {
                dtos.Add(new DslDiagnosticDto
                {
                    Line = d.Line,
                    Column = d.Column,
                    Length = d.Length,       
                    Message = d.Message,
                    Severity = d.Severity
                });
            }

            return new DslValidationResultDto
            {
                IsValid = dtos.Count == 0,
                Diagnostics = dtos.ToArray()
            };
        }





        public DslCompletionListDto GetCompletions(string code, int caretOffset, IEditorInteractive editor, IDslContextProvider ctxProvider)
        {
            // Deleguj do wspólnego providera (użyj false dla command mode – dostosuj jeśli potrzebujesz dynamicznego)
            var list = AntlrCompletionProvider.GetCompletions(code, caretOffset,editor, ctxProvider);
            return new DslCompletionListDto { Items = list.Items.Select(i => new DslCompletionItemDto { Label = i.Label, InsertText = i.InsertText }).ToArray(), IsIncomplete = list.IsIncomplete };
        }
  

        // Poprawiona klasa listener (dziedziczy po BaseErrorListener dla parsera, implementuje IAntlrErrorListener<int> dla lexera)
        private class SilentErrorListener : BaseErrorListener, IAntlrErrorListener<int>
        {
            public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                // Cicho dla parser errors (IToken)
            }

            void IAntlrErrorListener<int>.SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                // Cicho dla lexer errors (int)
            }
        }



        private sealed class LabelComparer : System.Collections.Generic.IEqualityComparer<DslCompletionItemDto>
        {
            public bool Equals(DslCompletionItemDto x, DslCompletionItemDto y)
            {
                var a = x != null ? x.Label : null;
                var b = y != null ? y.Label : null;
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(DslCompletionItemDto obj)
            {
                var s = (obj != null && obj.Label != null) ? obj.Label.ToLowerInvariant() : string.Empty;
                return s.GetHashCode();
            }
        }

        public DslSemanticLegendDto GetSemanticLegend()
        => Features.AntlrSemanticTokensProvider.GetLegend();

        public DslSemanticTokensDto GetSemanticTokens(string code)
            => Features.AntlrSemanticTokensProvider.GetTokens(
                   code ?? string.Empty,
                   type => PhialeDslLexer.DefaultVocabulary.GetSymbolicName(type) ?? string.Empty);


    }


}
