using Moq;
using NUnit.Framework;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Core.Graphics;
using PhialeGis.Library.Core.Models.Geometry;


namespace PhialeGis.Library.Tests.Models.DeviceRepresentation
{
    [TestFixture]
    [Category("Unit")]
    internal class ViewportManager_Test
    {
        private const double MaxRectangleViewPortCoord = 100;
        private const double IgnoredValue = 1;
        private ViewportManager viewportManager;
        private PhTransformParams currentParrams;

        [SetUp]
        public void SetUp()
        {
            var mockDevice = new Mock<IDevice>();
            mockDevice.Setup(device => device.GetDpiX()).Returns(96.0);
            mockDevice.Setup(device => device.GetDpiY()).Returns(96.0);
            mockDevice.SetupGet(device => device.CurrentWidth).Returns(100);
            mockDevice.SetupGet(device => device.CurrentHeight).Returns(100.0);
           // var device = new DeviceDecorator(mockDevice.Object);
            viewportManager = new ViewportManager(mockDevice.Object);
            currentParrams = viewportManager.Params;
            currentParrams = new PhTransformParams
            {
                ActiveWindow = new PhRect(0, 0, 100, 100)
                {
                    Emin = new PhPoint(0, 0),
                    Emax = new PhPoint(100, 100)
                },
                Scale = 1
            };
            viewportManager.Params = currentParrams;
            viewportManager.DeviceWindow = new PhRect(0, 0, 100, 100);
        }

        [TestCase(20, 60, 20, 40, 1)]
        [TestCase(40, 35, 80, 30, 2)]
        public void PointToDevice(double shapeX, double shapeY, double canvasX, double canvasY, double scale)
        {
            var currentRectangleViewportCoord = MaxRectangleViewPortCoord / scale;
            currentParrams.ActiveWindow.Emin = new PhPoint(0, 0);
            currentParrams.ActiveWindow.Emax = new PhPoint(currentRectangleViewportCoord, currentRectangleViewportCoord);
            currentParrams.Scale = scale;
            viewportManager.Params = currentParrams;// because of structure
            var resultPoint = viewportManager.PointToDevice(new PhPoint(shapeX, shapeY));

            Assert.AreEqual(canvasX, resultPoint.X);
            Assert.AreEqual(canvasY, resultPoint.Y);
        }

        [TestCase(20, 60, 20, 40, 1)]
        [TestCase(40, 35, 80, 30, 2)]
        [Test]
        public void TranslatePoint(double shapeX, double shapeY, double canvasX, double canvasY, double scale)
        {
            var currentRectangleViewportCoord = MaxRectangleViewPortCoord / scale;

            currentParrams.ActiveWindow.Emin = new PhPoint(0, 0);
            currentParrams.ActiveWindow.Emax = new PhPoint(currentRectangleViewportCoord, currentRectangleViewportCoord);
            currentParrams.Scale = scale;
            viewportManager.Params = currentParrams;
            var resultPoint = viewportManager.TranslatePoint(new Point(canvasX, canvasY));

            Assert.AreEqual(shapeX, resultPoint.X);
            Assert.AreEqual(shapeY, resultPoint.Y);
        }

        [Test]
        public void TranslateRect()
        {
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(100, 100));
            var pmin = new Point(0, 0);
            var pmax = new Point(40, 50);
            var rect = new Rect(pmin, pmax);
            var resultRect = viewportManager.TranslateRect(rect);

            Assert.IsTrue(GeometryUtils.EqualPoint(resultRect.Emin, new PhPoint(0, 50)));
            Assert.IsTrue(GeometryUtils.EqualPoint(resultRect.Emax, new PhPoint(40, 100)));
        }

        [Test]
        public void RectToDevice()
        {
            var modelRect = new PhRect(new PhPoint(60, 15), new PhPoint(75, 30));

            var resultRect = viewportManager.RectToDevice(modelRect);

            Assert.AreEqual(resultRect.Left, 60);
            Assert.AreEqual(resultRect.Top, 70);
            Assert.AreEqual(resultRect.Right, 75);
            Assert.AreEqual(resultRect.Bottom, 85);
        }

        [Test]
        [TestCase(96, 7462837.8264568, 7462669.05096938, 1342, 12.0733582658579)]
        public void TranslateDistXCoord(int distance, double xmax, double xmin, double deviceWidth, double result)
        {
            currentParrams.ActiveWindow.Emin = new PhPoint(xmin, IgnoredValue);
            currentParrams.ActiveWindow.Emax = new PhPoint(xmax, IgnoredValue);
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(deviceWidth, IgnoredValue));
            viewportManager.Params = currentParrams;
            var calcDist = viewportManager.TranslateDistXCoord(distance);

            Assert.AreEqual(result, calcDist, 0.000000001);
        }

        [Test]
        [TestCase(12.0733582658579, 7462837.8264568, 7462669.05096938, 1342, 96)]
        public void DistToDeviceXCoord(double distance, double xmax, double xmin, double deviceWidth, double result)
        {
            currentParrams.ActiveWindow.Emin = new PhPoint(xmin, IgnoredValue);
            currentParrams.ActiveWindow.Emax = new PhPoint(xmax, IgnoredValue);
            currentParrams.Scale = IgnoredValue;
            viewportManager.Params = currentParrams;
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(deviceWidth, IgnoredValue));

            var calcDist = viewportManager.DistToDeviceXCoord(distance);
            Assert.AreEqual(result, calcDist);
        }

        [Test]
        [TestCase(6, 5962794.03205814, 5962693.99150277, 775, 0.774507525438263)]
        public void TranslateDistYCoord(int distance, double ymax, double ymin, double deviceHeight, double result)
        {
            currentParrams.ActiveWindow.Emin = new PhPoint(IgnoredValue, ymin);
            currentParrams.ActiveWindow.Emax = new PhPoint(IgnoredValue, ymax);
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(IgnoredValue, deviceHeight));
            viewportManager.Params = currentParrams;
            var calcDist = viewportManager.TranslateDistYCoord(distance);

            Assert.AreEqual(result, calcDist, 0.000000001);
        }

        [Test]
        [TestCase(0.774507525438263, 5962794.03205814, 5962693.99150277, 775, 6)]
        public void DistToDeviceYCoord(double distance, double ymax, double ymin, double deviceHeight, int result)
        {
            currentParrams.ActiveWindow.Emin = new PhPoint(IgnoredValue, ymin);
            currentParrams.ActiveWindow.Emax = new PhPoint(IgnoredValue, ymax);
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(IgnoredValue, deviceHeight));
            viewportManager.Params = currentParrams;
            var calcDist = viewportManager.DistToDeviceYCoord(distance);

            Assert.AreEqual(result, calcDist, 0.000000001);
        }

        [Test]
        [TestCase(9, 5962816.36369179, 5962716.32313642, 775, 1.54901505087653)]
        public void PtToDeviceYCoord(double distance, double ymax, double ymin, double deviceHeight, double result)
        {
            currentParrams.ActiveWindow.Emin = new PhPoint(IgnoredValue, ymin);
            currentParrams.ActiveWindow.Emax = new PhPoint(IgnoredValue, ymax);
            currentParrams.Scale = IgnoredValue;
            viewportManager.Params = currentParrams;
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(IgnoredValue, deviceHeight));

            var calcDist = viewportManager.PtToDeviceYCoord(distance);

            Assert.AreEqual(result, calcDist, 0.000000001);
        }

        [Test]
        [TestCase(9, 5962816.36369179, 5962716.32313642, 775, 1.54901505087653)]
        public void PtToDeviceXCoord(double distance, double xmax, double xmin, double deviceWidth, double result)
        {
            currentParrams.ActiveWindow.Emin = new PhPoint(xmin, IgnoredValue);
            currentParrams.ActiveWindow.Emax = new PhPoint(xmax, IgnoredValue);
            currentParrams.Scale = IgnoredValue;
            viewportManager.Params = currentParrams;
            viewportManager.DeviceWindow = new PhRect(new PhPoint(0, 0), new PhPoint(deviceWidth, IgnoredValue));

            var calcDist = viewportManager.PtToDeviceXCoord(distance);

            Assert.AreEqual(result, calcDist, 0.000000001);
        }

        [Test]
        public void ApplyActiveView()
        {
            PhRect finalRect = new PhRect(x1: 7462679.4623417, y1: 5962698.4271238931, x2: 7462828.50234887, y2: 5962793.96559003);
            PhPoint finalCentroid = new PhPoint(x: 7462753.9823452849, y: 5962746.1963569615);

            PhRect rect = new PhRect(x1: 7462665.53221697, y1: 5962698.55039934,
                                      x2: 7462814.57222413, y2: 5962794.08886547);

            PhPoint centroid = new PhPoint(
                x: 7462740.05222055, y: 5962746.31963241);

            double scale = 8.11191587428143;

            var newParams = new PhTransformParams()
            {
                ActiveWindow = rect,
                Centroid = centroid,
                Scale = scale
            };
            viewportManager.Params = newParams;

            PhRect pretendentRect = new PhRect(x1: 7462679.4623417, y1: 5962698.4271239, x2: 7462828.50234887, y2: 5962793.96559003);

            viewportManager.ApplyActiveView(pretendentRect);

            Assert.IsTrue(GeometryUtils.EqualRect2D(viewportManager.ActiveWindow, finalRect));
            Assert.IsTrue(GeometryUtils.EqualPoint(viewportManager.Params.Centroid, finalCentroid));
        }

        [Test]
        public void Zoom_ScalesWindowAndKeepsCenter()
        {
            var activeWindow = new PhRect(
                new PhPoint(7462679.42773078, 5962698.04540552),
                new PhPoint(7462829.06557023, 5962793.96709748));
            var scale = 8.07950719195074;
            var factor = 0.85;

            viewportManager.Params = new PhTransformParams
            {
                ActiveWindow = activeWindow,
                // Intentionally not equal to center to verify Zoom uses ActiveWindow center.
                Centroid = new PhPoint(0, 0),
                Scale = scale
            };

            var initialCenterX = (activeWindow.Emin.X + activeWindow.Emax.X) / 2.0;
            var initialCenterY = (activeWindow.Emin.Y + activeWindow.Emax.Y) / 2.0;
            var initialWidth = activeWindow.Emax.X - activeWindow.Emin.X;
            var initialHeight = activeWindow.Emax.Y - activeWindow.Emin.Y;

            var applied = viewportManager.Zoom(factor);

            Assert.That(applied, Is.True);
            Assert.That(viewportManager.Scale, Is.EqualTo(scale * factor).Within(1e-12));

            var result = viewportManager.ActiveWindow;
            var resultCenterX = (result.Emin.X + result.Emax.X) / 2.0;
            var resultCenterY = (result.Emin.Y + result.Emax.Y) / 2.0;
            var resultWidth = result.Emax.X - result.Emin.X;
            var resultHeight = result.Emax.Y - result.Emin.Y;

            Assert.That(resultCenterX, Is.EqualTo(initialCenterX).Within(1e-9));
            Assert.That(resultCenterY, Is.EqualTo(initialCenterY).Within(1e-9));
            Assert.That(resultWidth, Is.EqualTo(initialWidth / factor).Within(1e-9));
            Assert.That(resultHeight, Is.EqualTo(initialHeight / factor).Within(1e-9));
        }

        [Test]
        public void Zoom_WithNonPositiveFactor_DoesNotChangeState()
        {
            viewportManager.Params = new PhTransformParams
            {
                ActiveWindow = new PhRect(new PhPoint(10, 20), new PhPoint(110, 70)),
                Centroid = new PhPoint(60, 45),
                Scale = 3
            };

            var before = viewportManager.Params;

            var applied = viewportManager.Zoom(0);

            Assert.That(applied, Is.False);
            Assert.That(viewportManager.Scale, Is.EqualTo(before.Scale));
            Assert.That(GeometryUtils.EqualRect2D(viewportManager.ActiveWindow, before.ActiveWindow), Is.True);
            Assert.That(GeometryUtils.EqualPoint(viewportManager.Params.Centroid, before.Centroid), Is.True);
        }

        [Test]
        public void UpdateCentroid()
        {
            PhRect finalRect = new PhRect(
                new PhPoint(7462728.3130662553, 5962735.12571655),
                new PhPoint(7462769.4157299045, 5962761.47357787)
            );

            PhRect actWin = new PhRect(
              new PhPoint(7462726.45021747, 5962733.58084195),
              new PhPoint(7462767.55288112, 5962759.92870327));

            PhPoint centroid = new PhPoint(7462747.00154929, 5962746.75477261);

            double scale = 29.414152091478;

            var newParams = new PhTransformParams()
            {
                ActiveWindow = actWin,
                Centroid = centroid,
                Scale = scale
            };
            viewportManager.Params = newParams;

            PhPoint updatedPoint = new PhPoint(7462748.86439808, 5962748.29964721
            );

            viewportManager.UpdateCentroid(updatedPoint);

            Assert.IsTrue(GeometryUtils.EqualRect2D(viewportManager.ActiveWindow, finalRect));
        }
    }
}

