using NUnit.Framework;
using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Models.Parsing;
using System;

namespace PhialeGis.Library.Tests.Models.Parsing
{
    [TestFixture]
    [Category("Unit")]
    public class WktInterpreterTests
    {
        [TestCase("Linestring z(1 2 3, 2 3 4, 4 5 6, 7 8.1 9)", 6)]
        [TestCase("Linestring z(10 20 30, 20 30 40, 40 50 60, 70 80 90)", 60)]
        [TestCase("POLYGON ((30 10, 40 40, 20 40, 10 20, 30 10))", 30)]
        [TestCase("POLYGON ((5 5, 5 15, 15 15, 15 5, 5 5))", 10)]
        public void CreateGeometryFromWkt_ReturnsExpectedBoundingBoxWidth(string wkt, double expectedWidth)
        {
            var interpreter = CreateInterpreter();

            var geometry = interpreter.CreateGeometryFromWKT(wkt);
            var box = geometry.GetBoundingBox();

            Assert.That(box.Width, Is.EqualTo(expectedWidth).Within(1e-9));
        }

        [TestCase("POINT Z(10 20 30)", 10, 20, 30)]
        [TestCase("POINT Z(20 30 40)", 20, 30, 40)]
        public void CreateGeometryFromWkt_PointZ_ReturnsExpectedCoordinates(
            string wkt,
            double expectedX,
            double expectedY,
            double expectedZ)
        {
            var interpreter = CreateInterpreter();

            var geometry = interpreter.CreateGeometryFromWKT(wkt);
            Assert.That(geometry, Is.TypeOf<PhVector>(), "Expected parser to build PhVector.");

            var vector = (PhVector)geometry;
            Assert.That(vector[0].X, Is.EqualTo(expectedX).Within(1e-9));
            Assert.That(vector[0].Y, Is.EqualTo(expectedY).Within(1e-9));
            Assert.That(vector[0].Z, Is.EqualTo(expectedZ));
        }

        [TestCase("POLYGON ((10 10, 10 20, 20 20, 20 10, 10 10))", 1)]
        [TestCase("POLYGON ((0 0, 0 30, 30 30, 30 0, 0 0), (10 10, 10 20, 20 20, 20 10, 10 10))", 2)]
        [TestCase("POLYGON ((0 0, 0 50, 50 50, 50 0, 0 0), (10 10, 10 20, 20 20, 20 10, 10 10), (30 30, 30 40, 40 40, 40 30, 30 30))", 3)]
        public void CreateGeometryFromWkt_Polygon_ReturnsExpectedRingCount(string wkt, int expectedRingCount)
        {
            var interpreter = CreateInterpreter();

            var geometry = interpreter.CreateGeometryFromWKT(wkt);
            Assert.That(geometry, Is.TypeOf<PhVector>(), "Expected parser to build PhVector.");

            var vector = (PhVector)geometry;
            Assert.That(vector.PartCount, Is.EqualTo(expectedRingCount));
        }

        [Test]
        public void CreateGeometryFromWkt_Throws_For_WhitespaceInput()
        {
            var interpreter = CreateInterpreter();

            Assert.That(
                () => interpreter.CreateGeometryFromWKT("   "),
                Throws.TypeOf<ArgumentException>());
        }

        private static WktInterpreter CreateInterpreter()
        {
            var factory = new BaseGeometryFactory();
            var parser = new WKTParser(factory);
            return new WktInterpreter(parser);
        }
    }
}

