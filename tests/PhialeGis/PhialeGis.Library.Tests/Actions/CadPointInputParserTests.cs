using NUnit.Framework;
using PhialeGis.Library.Actions.Ogc;

namespace PhialeGis.Library.Tests.Actions
{
    [TestFixture]
    [Category("Unit")]
    public sealed class CadPointInputParserTests
    {
        [Test]
        public void ParsesAbsolutePoint()
        {
            var ok = CadPointInputParser.TryParse("10 20", false, 0, 0, out var p);
            Assert.IsTrue(ok);
            Assert.AreEqual(10, p.X, 1e-9);
            Assert.AreEqual(20, p.Y, 1e-9);
        }

        [Test]
        public void ParsesRelativePoint()
        {
            var ok = CadPointInputParser.TryParse("@5 -3", true, 10, 20, out var p);
            Assert.IsTrue(ok);
            Assert.AreEqual(15, p.X, 1e-9);
            Assert.AreEqual(17, p.Y, 1e-9);
        }

        [Test]
        public void ParsesPolarPoint()
        {
            var ok = CadPointInputParser.TryParse("<90 10>", true, 0, 0, out var p);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, p.X, 1e-6);
            Assert.AreEqual(10, p.Y, 1e-6);
        }

        [Test]
        public void UndoTokensRecognized()
        {
            Assert.IsTrue(CadPointInputParser.IsUndo("UNDO"));
            Assert.IsTrue(CadPointInputParser.IsUndo("u"));
            Assert.IsTrue(CadPointInputParser.IsUndo("cofnij"));
            Assert.IsTrue(CadPointInputParser.IsUndo("confij"));
        }
    }
}
