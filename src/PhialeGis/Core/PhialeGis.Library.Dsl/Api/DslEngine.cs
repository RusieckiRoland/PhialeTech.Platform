using Antlr4.Runtime;
using NetTopologySuite.Algorithm;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Modes;
using PhialeGis.Library.Dsl.Visitors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PhialeGis.Library.Dsl.Api
{
    /// <summary>
    /// Orchestrates parsing (ANTLR) and execution (MapContext) of DSL commands.
    /// </summary>
    public sealed class DslEngine
    {
        private readonly MapContext _ctx;

        /// <summary>
        /// Creates an engine bound to a concrete GIS model and viewport/graphics adapters.
        /// </summary>
        public DslEngine(PhGis gis, IViewport viewport, IGraphicsFacade graphics)
        {
            if (gis == null) throw new ArgumentNullException(nameof(gis));
            if (viewport == null) throw new ArgumentNullException(nameof(viewport));
            if (graphics == null) throw new ArgumentNullException(nameof(graphics));

            _ctx = new MapContext(gis, viewport, graphics);
        }

        /// <summary>
        /// Parses and executes a single-line DSL command. Returns diagnostics (if any).
        /// </summary>
        public async Task<DslResult> ParseAndExecuteAsync(string code, object target, IEditorInteractive source, IDslContextProvider ctxProvider)
        {
            return await Task.Run(delegate
            {
                var result = new DslResult();
                var diags = result.Diagnostics;

                try
                {
                    var request = ParseSingle(code ?? string.Empty, diags,source, ctxProvider);
                    if (request != null && diags.Count == 0)
                    {
                        _ctx.Execute(request,target, source); // execute in Core
                        result.Executed = true;
                    }
                }
                catch (Exception ex)
                {
                    diags.Add(new DslDiag("error", ex.Message));
                }

                return result;
            });
        }

        public void AttachManager(IGisInteractionManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));        
            _ctx.AttachManager(manager);
        }

 

        /// <summary>
        /// Runs the ANTLR pipeline: lexer → parser → parse tree → visitor.
        /// Returns the request object produced by the visitor (e.g., AddLayerRequest or ZoomRequest).
        /// </summary>
        private object ParseSingle(string code, List<DslDiag> diags, IEditorInteractive source, IDslContextProvider ctxProvider)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;
            var pre = UpperOutsideQuotes(code ?? string.Empty); //case insesitive pipe
           
            // ── fabryka DSL: jedno miejsce konfiguracji lexera/parsera
            var pipe = DslPipelineFactory.Create(pre, DslPipelinePurpose.Execute,source, ctxProvider);
            var parser = pipe.Parser;
            var lexer  = pipe.Lexer;

            // Collect lexer and parser errors into diagnostics.
            var err = new CollectingErrorListener(diags);
            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            lexer.AddErrorListener(err);   
            parser.AddErrorListener(err); 

            var entry = DslPipelineRegistry.Resolve(pipe.Context.Mode).entry;
            var tree = entry(parser);

            // Your custom visitor builds a neutral DTO request.
            var visitor = new PhialeVisitor();
            return visitor.Visit(tree); // typically AddLayerRequest or ZoomRequest
        }
        private static string UpperOutsideQuotes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var chars = input.ToCharArray();
            bool inString = false;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (c == '"')
                {
                    inString = !inString;     // przełączamy tryb
                    continue;
                }

                if (!inString)
                    chars[i] = char.ToUpperInvariant(c);
                // jeśli inString == true -> zostawiamy jak jest
            }
            return new string(chars);
        }
    }



    /// <summary>
    /// Result envelope for a parse/execute step.
    /// </summary>
    public sealed class DslResult
    {
        public bool Executed { get; set; }
        public List<DslDiag> Diagnostics { get; } = new List<DslDiag>();
    }

    /// <summary>
    /// Single diagnostic item (error/warning/info).
    /// </summary>
    public sealed class DslDiag
    {
        public DslDiag(string severity, string message, int line = 0, int column = 0, int length =1)
        {
            Severity = severity;
            Message = message;
            Line = line;
            Column = column;
            Length = Math.Max(1, length);
        }

        public string Severity { get; }
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        public int Length { get; } // >= 1

    }

    public sealed class CollectingErrorListener :
    IAntlrErrorListener<IToken>,   // parser errors
    IAntlrErrorListener<int>       // lexer errors (offending char code)
    {
        private readonly List<DslDiag> _diags;

        public CollectingErrorListener(List<DslDiag> diags)
        {
            _diags = diags ?? throw new ArgumentNullException(nameof(diags));
        }

        // PARSER: offending symbol is an IToken
        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
                        int line, int charPositionInLine, string msg, RecognitionException e)
        {
            int len = 1;
            if (offendingSymbol != null)
            {
                var text = offendingSymbol.Text;
                if (!string.IsNullOrEmpty(text))
                    len = Math.Max(1, text.Length);
                else if (offendingSymbol.StopIndex >= offendingSymbol.StartIndex)
                    len = (offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1);
            }

            _diags.Add(new DslDiag("error", msg, line, charPositionInLine + 1, len)); // +1 → 1-based
        }

        // LEXER: offending symbol is a char code (int)
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
                                int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _diags.Add(new DslDiag("error", msg, line, charPositionInLine + 1, 1));
        }
    }
}
