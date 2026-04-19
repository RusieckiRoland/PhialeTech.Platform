using NUnit.Framework;
using PhialeGis.Library.Core.Styling;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class DefaultStyleContrastPolicyTests
    {
        [Test]
        public void ShouldApplyHalo_WhenForegroundMatchesDarkBackground_ReturnsTrue()
        {
            var policy = new DefaultStyleContrastPolicy();

            var result = policy.ShouldApplyHalo(
                unchecked((int)0xFF0A1220u),
                unchecked((int)0xFF0A1220u));

            Assert.That(result, Is.True);
        }

        [Test]
        public void GetHaloColorArgb_OnDarkBackground_ReturnsLightHalo()
        {
            var policy = new DefaultStyleContrastPolicy();

            var halo = policy.GetHaloColorArgb(unchecked((int)0xFF0A1220u));

            Assert.That(unchecked((uint)halo), Is.EqualTo(0xE6FFFFFFu));
        }

        [Test]
        public void GetBorderColorArgb_OnLightBackground_ReturnsDarkBorder()
        {
            var policy = new DefaultStyleContrastPolicy();

            var border = policy.GetBorderColorArgb(unchecked((int)0xFFFFFFFFu));

            Assert.That(unchecked((uint)border), Is.EqualTo(0xFF163046u));
        }
    }
}
