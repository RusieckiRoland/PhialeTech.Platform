using Antlr4.Runtime;
using PhialeGis.Library.Abstractions.Modes;

namespace PhialeGis.Library.Dsl.Modes
{
    public sealed class DslPipeline
    {
        public PhialeDslLexer Lexer { get; }
        public CommonTokenStream Tokens { get; }
        public PhialeDslParser Parser { get; }
        public DslContext Context { get; }
        internal DslPipeline(PhialeDslLexer l, CommonTokenStream t, PhialeDslParser p, DslContext c)
        { Lexer = l; Tokens = t; Parser = p; Context = c; }
    }
}
