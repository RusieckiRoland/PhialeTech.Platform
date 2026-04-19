using System.Collections.Generic;
using System.Globalization;
using Antlr4.Runtime;
using NUnit.Framework;
using PhialeGis.Library.Dsl;
using PhialeGis.Library.Dsl.Requests;
using PhialeGis.Library.Dsl.Visitors;

namespace PhialeGis.Library.Tests.Dsl.Visitor
{
    [TestFixture]
    [Category("Unit")]
    public class DslVisitorTests
    {
        [Test]
        public void ZoomIn_Parses_To_ZoomInRequest()
        {
            var result = ParseSingleCommandAndAssertNoSyntaxErrors("ZOOMIN");
            Assert.That(result, Is.InstanceOf<ZoomInRequest>());
        }

        [Test]
        public void ZoomOut_Parses_To_ZoomOutRequest()
        {
            var result = ParseSingleCommandAndAssertNoSyntaxErrors("ZOOMOUT");
            Assert.That(result, Is.InstanceOf<ZoomOutRequest>());
        }

        [Test]
        public void AddLineString_Parses_To_AddLinestringRequest()
        {
            var result = ParseSingleCommandAndAssertNoSyntaxErrors("ADD LINESTRING");
            Assert.That(result, Is.InstanceOf<AddLinestringRequest>());
        }

        [Test]
        public void AddLineString_Is_CaseInsensitive()
        {
            var result = ParseSingleCommandAndAssertNoSyntaxErrors("add linestring");
            Assert.That(result, Is.InstanceOf<AddLinestringRequest>());
        }

        [Test]
        public void Zoom_Parses_Number_In_InvariantCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");

            try
            {
                var result = ParseSingleCommandAndAssertNoSyntaxErrors("ZOOM 1.5");
                Assert.That(result, Is.InstanceOf<ZoomRequest>());

                var request = (ZoomRequest)result;
                Assert.That(request.Value, Is.EqualTo(1.5).Within(1e-9));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Test]
        public void Zoom_Without_Value_Throws_FormatException()
        {
            Assert.That(
                () => ParseSingleCommand("ZOOM", out _),
                Throws.TypeOf<System.FormatException>());
        }

        private static object ParseSingleCommandAndAssertNoSyntaxErrors(string code)
        {
            var result = ParseSingleCommand(code, out var syntaxErrors);

            Assert.That(
                syntaxErrors,
                Is.Empty,
                "Unexpected syntax errors: " + string.Join(" | ", syntaxErrors));

            return result;
        }

        private static object ParseSingleCommand(string code, out IReadOnlyList<string> syntaxErrors)
        {
            var preprocessed = UpperOutsideQuotes(code);

            var input = new AntlrInputStream(preprocessed);
            var lexer = new PhialeDslLexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PhialeDslParser(tokens);

            var errorListener = new CollectingErrorListener();
            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);
            parser.AddErrorListener(errorListener);

            var tree = parser.command();
            var visitor = new PhialeVisitor();
            var result = visitor.Visit(tree);

            syntaxErrors = errorListener.Errors;
            return result;
        }

        private static string UpperOutsideQuotes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var chars = input.ToCharArray();
            var inString = false;

            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                    chars[i] = char.ToUpperInvariant(c);
            }

            return new string(chars);
        }

        private sealed class CollectingErrorListener :
            IAntlrErrorListener<IToken>,
            IAntlrErrorListener<int>
        {
            private readonly List<string> _errors = new List<string>();

            public IReadOnlyList<string> Errors => _errors;

            public void SyntaxError(
                System.IO.TextWriter output,
                IRecognizer recognizer,
                IToken offendingSymbol,
                int line,
                int charPositionInLine,
                string msg,
                RecognitionException e)
            {
                _errors.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "line {0}:{1} {2}",
                    line,
                    charPositionInLine,
                    msg));
            }

            public void SyntaxError(
                System.IO.TextWriter output,
                IRecognizer recognizer,
                int offendingSymbol,
                int line,
                int charPositionInLine,
                string msg,
                RecognitionException e)
            {
                _errors.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "line {0}:{1} {2}",
                    line,
                    charPositionInLine,
                    msg));
            }
        }
    }
}
