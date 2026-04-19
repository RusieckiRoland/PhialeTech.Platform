// PhialeGis.Library.Dsl/DslPipelineRegistry.cs
using Antlr4.Runtime.Tree;
using PhialeGis.Library.Abstractions.Modes;
using System;

namespace PhialeGis.Library.Dsl.Modes
{
    public static class DslPipelineRegistry
    {
        // Zwraca tuple: (lexerModeId, entryRuleInvoker)
        public static (int? lexerMode, Func<PhialeDslParser, IParseTree> entry, string startRuleName) Resolve(DslMode mode)
        {
            
            switch (mode)
            {
                case DslMode.Points:
                    return (PhialeDslLexer.POINTS, p => p.pointsLine(), "pointsLine");
                case DslMode.Normal:
                default:
                    return (null, p => p.command(), "command");
            }
        }
    }
}
