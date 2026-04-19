using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Interactions.Dsl;
using PhialeGis.Library.Abstractions.Localization;
using PhialeGis.Library.Abstractions.Modes;
using PhialeGis.Library.Dsl;
using PhialeGis.Library.Dsl.Modes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeGis.Library.DslEditor.Features
{
    internal static class AntlrCompletionProvider
    {
        public static DslCompletionListDto GetCompletions(
            string text,
            int caret,
            IEditorInteractive editor,
            IDslContextProvider ctxProvider)
        {
            if (text == null) text = string.Empty;
            if (caret < 0 || caret > text.Length) caret = text.Length;

            var left = text.Substring(0, caret);
            var prefix = ComputePrefix(left);
            var leftTrimmed = prefix.Length > 0 ? left.Substring(0, left.Length - prefix.Length) : left;

            var pipe = DslPipelineFactory.Create(leftTrimmed, DslPipelinePurpose.Completions, editor, ctxProvider);
            var parser = pipe.Parser;
            var lexer = pipe.Lexer;
            var map = DslPipelineRegistry.Resolve(pipe.Context.Mode);

            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            parser.BuildParseTree = false;
            parser.Interpreter.PredictionMode = PredictionMode.LL;

            var strategy = new CompletionErrorStrategy();
            parser.ErrorHandler = strategy;

            try
            {
                map.entry(parser);
            }
            catch (ParseCanceledException)
            {
                // Expected while requesting completion on partial input.
            }
            catch (RecognitionException)
            {
                // Keep best-effort behavior.
            }

            IntervalSet expected = strategy.ExpectedTokens;
            if (expected == null)
            {
                try { expected = parser.GetExpectedTokens(); }
                catch { expected = null; }
            }

            var languageId = DslUiLocalization.NormalizeLanguageId(pipe.Context?.LanguageId);
            var items = BuildItems(expected, parser.Vocabulary, prefix, languageId);
            if (items.Length == 0)
            {
                var startExpected = CollectExpectedAtRuleStart(parser, map.startRuleName);
                items = BuildItems(startExpected, parser.Vocabulary, prefix, languageId);
            }

            return new DslCompletionListDto
            {
                Items = items,
                IsIncomplete = false
            };
        }

        private static DslCompletionItemDto[] BuildItems(
            IntervalSet expected,
            IVocabulary vocabulary,
            string prefix,
            string languageId)
        {
            if (expected == null || vocabulary == null)
                return Array.Empty<DslCompletionItemDto>();

            var tokenTypes = expected.ToArray();
            if (tokenTypes == null || tokenTypes.Length == 0)
                return Array.Empty<DslCompletionItemDto>();

            var items = new List<DslCompletionItemDto>(tokenTypes.Length);
            for (int i = 0; i < tokenTypes.Length; i++)
            {
                var type = tokenTypes[i];
                if (type <= 0 || type == TokenConstants.EOF) continue;

                var item = CreateCompletionItem(vocabulary, type, languageId);
                if (item == null) continue;

                if (!MatchesPrefix(prefix, item.Label, item.InsertText))
                    continue;

                items.Add(item);
            }

            return items
                .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static DslCompletionItemDto CreateCompletionItem(IVocabulary vocabulary, int tokenType, string languageId)
        {
            var rawLabel = GetTokenLabel(vocabulary, tokenType);
            if (string.IsNullOrWhiteSpace(rawLabel))
                return null;

            var localized = DslUiLocalization.TryGetCompletion(rawLabel, languageId);
            if (localized != null)
            {
                return new DslCompletionItemDto
                {
                    Label = localized.Label ?? string.Empty,
                    InsertText = localized.InsertText ?? string.Empty,
                    Kind = string.IsNullOrWhiteSpace(localized.Kind) ? "text" : localized.Kind
                };
            }

            var insert = ShouldAppendSpace(rawLabel) ? rawLabel + " " : rawLabel;
            return new DslCompletionItemDto
            {
                Label = rawLabel,
                InsertText = insert
            };
        }

        private static IntervalSet CollectExpectedAtRuleStart(Parser parser, string startRuleName)
        {
            if (parser == null || string.IsNullOrWhiteSpace(startRuleName))
                return null;

            var ruleNames = parser.RuleNames;
            if (ruleNames == null || ruleNames.Length == 0)
                return null;

            var ruleIndex = Array.IndexOf(ruleNames, startRuleName);
            if (ruleIndex < 0)
                return null;

            var atn = parser.Atn;
            if (atn == null || atn.ruleToStartState == null || ruleIndex >= atn.ruleToStartState.Length)
                return null;

            var startState = atn.ruleToStartState[ruleIndex];
            if (startState == null)
                return null;

            var result = new IntervalSet();
            var visited = new HashSet<int>();
            TraverseForTerminalTransitions(startState, visited, result);

            return result;
        }

        private static void TraverseForTerminalTransitions(
            ATNState state,
            HashSet<int> visited,
            IntervalSet sink)
        {
            if (state == null || sink == null || visited == null)
                return;

            if (!visited.Add(state.stateNumber))
                return;

            for (var i = 0; i < state.NumberOfTransitions; i++)
            {
                var transition = state.Transition(i);
                if (transition == null)
                    continue;

                if (transition.IsEpsilon)
                {
                    TraverseForTerminalTransitions(transition.target, visited, sink);
                    continue;
                }

                var label = transition.Label;
                if (label != null)
                    sink.AddAll(label);
            }
        }

        private static string GetTokenLabel(IVocabulary vocabulary, int tokenType)
        {
            var literal = vocabulary.GetLiteralName(tokenType);
            if (!string.IsNullOrEmpty(literal))
            {
                if (literal.Length >= 2 && literal[0] == '\'' && literal[literal.Length - 1] == '\'')
                    return literal.Substring(1, literal.Length - 2);
                return literal;
            }

            return vocabulary.GetSymbolicName(tokenType) ?? string.Empty;
        }

        private static bool ShouldAppendSpace(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            var ch = label[0];
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        private static bool MatchesPrefix(string prefix, string label, string insert)
        {
            if (string.IsNullOrEmpty(prefix))
                return true;

            if (!string.IsNullOrEmpty(label) &&
                label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(insert) &&
                insert.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(label))
                return false;

            var parts = label.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ComputePrefix(string left)
        {
            if (string.IsNullOrEmpty(left)) return string.Empty;

            var i = left.Length - 1;
            while (i >= 0)
            {
                var ch = left[i];
                if (!(char.IsLetterOrDigit(ch) || ch == '_')) break;
                i--;
            }

            return left.Substring(i + 1);
        }

        private sealed class CompletionErrorStrategy : DefaultErrorStrategy
        {
            public IntervalSet ExpectedTokens { get; private set; }

            public override void Recover(Parser recognizer, RecognitionException e)
            {
                Capture(recognizer, e);
                throw new ParseCanceledException(e);
            }

            public override IToken RecoverInline(Parser recognizer)
            {
                Capture(recognizer, null);
                throw new ParseCanceledException();
            }

            public override void Sync(Parser recognizer)
            {
                // Avoid aggressive recovery in completion mode.
            }

            private void Capture(Parser recognizer, RecognitionException e)
            {
                if (ExpectedTokens != null) return;

                try { ExpectedTokens = recognizer.GetExpectedTokens(); }
                catch { ExpectedTokens = null; }
            }
        }
    }
}
