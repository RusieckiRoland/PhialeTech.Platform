// PhialeGis.Library.Dsl/DslPipelineFactory.cs
using Antlr4.Runtime;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Dsl.Modes;
using System;

namespace PhialeGis.Library.Dsl
{
       

    public static class DslPipelineFactory
    {
        public static DslPipeline Create(string text, DslPipelinePurpose purpose, IEditorInteractive source, IDslContextProvider ctxProvider)
        {

            if (source == null)  throw new ArgumentNullException(nameof(source));
            if (ctxProvider == null) throw new ArgumentNullException(nameof(ctxProvider));

            var ctx = ctxProvider.GetFor(source);
            var normalizedText = NormalizeTextForMode(text, ctx.Mode);
            var input = new AntlrInputStream(normalizedText);
            var lexer = new PhialeDslLexer(input);

            var map = DslPipelineRegistry.Resolve(ctx.Mode);
            if (map.lexerMode.HasValue)
                lexer.PushMode(map.lexerMode.Value);

            var tokens = new CommonTokenStream(lexer);
            var parser = new PhialeDslParser(tokens)
            {
                BuildParseTree = (purpose != DslPipelinePurpose.Validate)
            };

            return new DslPipeline(lexer, tokens, parser, ctx);
        }

        // Keep points-mode aliases parser-friendly so validation/completion
        // accepts the same undo words as the interactive action layer.
        private static string NormalizeTextForMode(string text, DslMode mode)
        {
            var raw = text ?? string.Empty;
            if (mode != DslMode.Points) return raw;

            var trimmed = raw.Trim();
            if (IsUndoAlias(trimmed))
                return "UNDO";

            return raw;
        }

        private static bool IsUndoAlias(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            return string.Equals(token, "UNDO", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "U", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "COFNIJ", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "CONFIJ", StringComparison.OrdinalIgnoreCase);
        }
    }
}
