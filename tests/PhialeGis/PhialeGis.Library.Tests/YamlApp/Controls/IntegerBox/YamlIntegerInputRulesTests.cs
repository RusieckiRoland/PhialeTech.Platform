using NUnit.Framework;
using PhialeTech.YamlApp.Core.Controls.IntegerBox;

namespace PhialeGis.Library.Tests.YamlApp.Controls.IntegerBox
{
    [TestFixture]
    public sealed class YamlIntegerInputRulesTests
    {
        [Test]
        public void IsCandidateValid_ShouldAllowDigits_WhenValueIsInRange()
        {
            Assert.That(YamlIntegerInputRules.IsCandidateValid("123", 0, 999), Is.True);
        }

        [Test]
        public void IsCandidateValid_ShouldRejectLetters()
        {
            Assert.That(YamlIntegerInputRules.IsCandidateValid("12a", null, null), Is.False);
        }

        [Test]
        public void IsCandidateValid_ShouldRejectMinus_WhenNegativeValuesAreNotAllowed()
        {
            Assert.That(YamlIntegerInputRules.IsCandidateValid("-", 0, null), Is.False);
            Assert.That(YamlIntegerInputRules.IsCandidateValid("-12", 0, null), Is.False);
        }

        [Test]
        public void IsCandidateValid_ShouldAllowStandaloneMinus_WhenNegativeValuesAreAllowed()
        {
            Assert.That(YamlIntegerInputRules.IsCandidateValid("-", -100, 100), Is.True);
        }

        [Test]
        public void IsCandidateValid_ShouldRejectValueOutsideConfiguredRange()
        {
            Assert.That(YamlIntegerInputRules.IsCandidateValid("150", 0, 100), Is.False);
            Assert.That(YamlIntegerInputRules.IsCandidateValid("-11", -10, 100), Is.False);
        }

        [Test]
        public void BuildCandidate_ShouldReplaceCurrentSelection()
        {
            var candidate = YamlIntegerInputRules.BuildCandidate("12345", 1, 3, "9");

            Assert.That(candidate, Is.EqualTo("195"));
        }
    }
}
