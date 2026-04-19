using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using PhialeGis.Library.Abstractions.Interactions.Dsl;

namespace PhialeGis.Library.Dsl.Features
{
    /// <summary>
    /// Single source of truth for Monaco/LSP semantic tokens.
    /// Maps ANTLR tokens to (type, modifiers) and encodes LSP delta stream.
    /// </summary>
    internal static class AntlrSemanticTokensProvider
    {
        // Stable, indexed legend. Indices must match returned positions.
        private static readonly string[] _types = new[]
        {
            "keyword","operator","number","string","comment","identifier","function","type","variable","namespace"
        };

        private static readonly string[] _mods = new[]
        {
            "declaration","readonly","static","deprecated","abstract"
        };

        public static DslSemanticLegendDto GetLegend() => new DslSemanticLegendDto
        {
            TokenTypes = _types,
            TokenModifiers = _mods
        };

        public static DslSemanticTokensDto GetTokens(string code, Func<int, string> tokenNameOf)
        {
            // Lexer
            var input = new AntlrInputStream(code ?? string.Empty);
            var lexer = new PhialeDslLexer(input); // your generated lexer
            var tokens = lexer.GetAllTokens();     // includes hidden channel

            var data = new List<int>(tokens.Count * 5);
            int prevLine = 0;   // 0-based for LSP deltas
            int prevStart = 0;

            foreach (var t in tokens)
            {
                // Skip whitespace; include comments if your grammar puts them on HIDDEN but you want to color them
                if (t.Channel == Lexer.Hidden)
                {
                    // if you want comment highlighting and comments are on HIDDEN, detect by token name here and include
                    var tn = tokenNameOf(t.Type);
                    var maybeComment = IsComment(tn);
                    if (!maybeComment) continue;
                }

                var tnName = tokenNameOf(t.Type);
                var typeIdx = MapType(tnName);
                if (typeIdx < 0) typeIdx = _typesIndexOf("identifier"); // fall back

                int modifiers = 0; // set bits if needed (e.g., declaration)
                // Example: if (IsDeclaration(tnName)) modifiers |= 1 << IndexOfMod("declaration");

                // ANTLR lines are 1-based; columns are 0-based
                int line0 = t.Line - 1;
                int col0 = t.Column;
                int length = (t.StopIndex >= t.StartIndex && t.Text != null)
                    ? (t.StopIndex - t.StartIndex + 1)
                    : (t.Text?.Length ?? 0);

                int deltaLine = line0 - prevLine;
                int deltaStart = (deltaLine == 0) ? (col0 - prevStart) : col0;

                data.Add(deltaLine);
                data.Add(deltaStart);
                data.Add(Math.Max(0, length));
                data.Add(typeIdx);
                data.Add(modifiers);

                prevLine = line0;
                prevStart = col0;
            }

            return new DslSemanticTokensDto { Data = data.ToArray(), ResultId = null };
        }

        private static int _typesIndexOf(string name) => Array.IndexOf(_types, name);

        private static int MapType(string tokenName)
        {
            // Map lexer symbolic names to legend indices.
            // Adjust to your grammar token names:
            // e.g. KW_ZOOM, KW_ADDLAYER, KW_STYLE → keyword
            if (tokenName.StartsWith("KW_")) return _typesIndexOf("keyword");

            switch (tokenName)
            {
                case "INT": case "NUMBER": return _typesIndexOf("number");
                case "STRING": case "SQ_STRING": case "DQ_STRING": return _typesIndexOf("string");
                case "LINE_COMMENT": case "BLOCK_COMMENT": return _typesIndexOf("comment");
                case "ID": case "IDENTIFIER": return _typesIndexOf("identifier");
                case "LPAREN":
                case "RPAREN":
                case "COMMA":
                case "PLUS":
                case "MINUS":
                case "STAR":
                case "SLASH":
                case "EQUALS":
                    return _typesIndexOf("operator");
                default:
                    return _typesIndexOf("identifier");
            }
        }

        private static bool IsComment(string tokenName)
            => tokenName == "LINE_COMMENT" || tokenName == "BLOCK_COMMENT";
    }
}
