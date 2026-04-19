using NUnit.Framework;
using PhialeGis.Library.Core.Enums;
using PhialeGis.Library.Core.Models.Geometry;

namespace PhialeGis.Library.Tests.Models.Geometry
{
    [TestFixture]
    [Category("Unit")]
    public class GeometryUtilsTests
    {
        [Test]
        public void NormalizeRectangle_Reorders_Inverted_Corners()
        {
            var rect = new PhRect(new PhPoint(10, 20), new PhPoint(-5, 3));

            var normalized = GeometryUtils.NormalizeRectangle(rect);

            Assert.That(normalized.Emin.X, Is.EqualTo(-5));
            Assert.That(normalized.Emin.Y, Is.EqualTo(3));
            Assert.That(normalized.Emax.X, Is.EqualTo(10));
            Assert.That(normalized.Emax.Y, Is.EqualTo(20));
        }

        [Test]
        public void AddOffset_Moves_Both_Corners_By_Given_Delta()
        {
            var rect = new PhRect(new PhPoint(1, 2), new PhPoint(4, 6));

            GeometryUtils.AddOffset(ref rect, 3, -2);

            Assert.That(rect.Emin.X, Is.EqualTo(4));
            Assert.That(rect.Emin.Y, Is.EqualTo(0));
            Assert.That(rect.Emax.X, Is.EqualTo(7));
            Assert.That(rect.Emax.Y, Is.EqualTo(4));
        }

        [Test]
        public void LineRel_For_Crossing_Lines_Returns_Intersection_Inside_Both_Segments()
        {
            var relation = GeometryUtils.LineRel(
                new PhPoint(0, 0),
                new PhPoint(10, 10),
                new PhPoint(0, 10),
                new PhPoint(10, 0),
                out var intersection);

            Assert.That(HasFlag(relation, LineRelations.Parallel), Is.False);
            Assert.That(HasFlag(relation, LineRelations.BetweenDiv), Is.True);
            Assert.That(HasFlag(relation, LineRelations.BetweenPar), Is.True);
            Assert.That(intersection.X, Is.EqualTo(5).Within(1e-9));
            Assert.That(intersection.Y, Is.EqualTo(5).Within(1e-9));
        }

        [Test]
        public void LineRel_For_Parallel_Lines_Sets_Parallel_Flag()
        {
            var relation = GeometryUtils.LineRel(
                new PhPoint(0, 0),
                new PhPoint(10, 0),
                new PhPoint(0, 5),
                new PhPoint(10, 5),
                out _);

            Assert.That(HasFlag(relation, LineRelations.Parallel), Is.True);
            Assert.That(HasFlag(relation, LineRelations.BetweenDiv), Is.False);
            Assert.That(HasFlag(relation, LineRelations.BetweenPar), Is.False);
        }

        private static bool HasFlag(LineRelations value, LineRelations flag)
        {
            return (value & flag) == flag;
        }
    }
}
