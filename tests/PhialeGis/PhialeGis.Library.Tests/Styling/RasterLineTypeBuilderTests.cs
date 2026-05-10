using System.Linq;
using NUnit.Framework;
using PhialeGis.Library.Core.Styling;

namespace PhialeGis.Library.Tests.Styling
{
    [TestFixture]
    public sealed class RasterLineTypeBuilderTests
    {
        [Test]
        public void BuildFromArgb32_CreatesLanesWithCenteredOffsets()
        {
            var builder = new RasterLineTypeBuilder();
            var pixels = new[]
            {
                unchecked((int)0x00000000u), unchecked((int)0xFF000000u), unchecked((int)0x00000000u),
                unchecked((int)0xFF000000u), unchecked((int)0x00000000u), unchecked((int)0xFF000000u),
                unchecked((int)0x00000000u), unchecked((int)0xFF000000u), unchecked((int)0x00000000u)
            };

            var pattern = builder.BuildFromArgb32(3, 3, pixels);

            Assert.That(pattern.Lanes.Count, Is.EqualTo(3));
            Assert.That(pattern.Lanes.Select(x => x.OffsetY), Is.EqualTo(new[] { -1d, 0d, 1d }));
            Assert.That(pattern.Lanes[1].RunLengths, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(pattern.Lanes[1].StartsWithDash, Is.True);
        }

        [Test]
        public void BuildFromArgb32_SkipsFullyTransparentRows()
        {
            var builder = new RasterLineTypeBuilder();
            var pixels = new[]
            {
                unchecked((int)0x00000000u), unchecked((int)0x00000000u), unchecked((int)0x00000000u),
                unchecked((int)0xFF000000u), unchecked((int)0xFF000000u), unchecked((int)0x00000000u)
            };

            var pattern = builder.BuildFromArgb32(3, 2, pixels);

            Assert.That(pattern.Lanes.Count, Is.EqualTo(1));
            Assert.That(pattern.Lanes[0].OffsetY, Is.EqualTo(0.5d));
            Assert.That(pattern.Lanes[0].RunLengths, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(pattern.Lanes[0].StartsWithDash, Is.True);
        }

        [Test]
        public void BuildFromArgb32_UsesAlphaThresholdToClassifyVisiblePixels()
        {
            var builder = new RasterLineTypeBuilder();
            var pixels = new[]
            {
                unchecked((int)0x08000000u), unchecked((int)0x20000000u), unchecked((int)0x08000000u)
            };

            var pattern = builder.BuildFromArgb32(3, 1, pixels, alphaThreshold: 16);

            Assert.That(pattern.Lanes.Count, Is.EqualTo(1));
            Assert.That(pattern.Lanes[0].StartsWithDash, Is.False);
            Assert.That(pattern.Lanes[0].RunLengths, Is.EqualTo(new[] { 1, 1, 1 }));
        }
    }
}

