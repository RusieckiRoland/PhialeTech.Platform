using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeGrid.Core.Layout;
using PhialeGrid.Core.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridSurfaceAccessibilityTests
    {
        [Test]
        public void GridSurfaceHost_ExposesDataGridAutomationPeer()
        {
            var coordinator = new global::PhialeGrid.Core.GridSurfaceCoordinator();
            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Name", Width = 120 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 24 },
                });
            coordinator.SetViewportSize(320, 200);

            var host = new GridSurfaceHost();
            host.Initialize(coordinator);
            var window = new Window
            {
                Width = 320,
                Height = 200,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };

            try
            {
                window.Show();
                FlushDispatcher(window.Dispatcher);

                var peer = UIElementAutomationPeer.CreatePeerForElement(host);

                Assert.Multiple(() =>
                {
                    Assert.That(peer, Is.Not.Null);
                    Assert.That(peer.GetAutomationControlType(), Is.EqualTo(AutomationControlType.DataGrid));
                    Assert.That(peer.GetName(), Is.Not.Empty);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void GridCellPresenter_ExposesDataItemAutomationMetadata()
        {
            var presenter = new GridCellPresenter
            {
                CellData = new GridCellSurfaceItem("row-1", "col-1", "row-1_col-1")
                {
                    DisplayText = "Alpha",
                    Bounds = new GridBounds(0, 0, 100, 24),
                },
                Bounds = new GridBounds(0, 0, 100, 24),
            };

            var peer = UIElementAutomationPeer.CreatePeerForElement(presenter);

            Assert.Multiple(() =>
            {
                Assert.That(peer, Is.Not.Null);
                Assert.That(peer.GetAutomationControlType(), Is.EqualTo(AutomationControlType.DataItem));
                Assert.That(peer.GetName(), Does.Contain("Alpha"));
            });
        }

        [Test]
        public void GridColumnHeaderPresenter_ExposesHeaderAutomationMetadata()
        {
            var presenter = new GridColumnHeaderPresenter
            {
                HeaderData = new GridHeaderSurfaceItem("col-1", GridHeaderKind.ColumnHeader)
                {
                    DisplayText = "Object Name",
                    Bounds = new GridBounds(0, 0, 120, 30),
                },
                Bounds = new GridBounds(0, 0, 120, 30),
            };

            var peer = UIElementAutomationPeer.CreatePeerForElement(presenter);

            Assert.Multiple(() =>
            {
                Assert.That(peer, Is.Not.Null);
                Assert.That(peer.GetAutomationControlType(), Is.EqualTo(AutomationControlType.HeaderItem));
                Assert.That(peer.GetName(), Does.Contain("Object Name"));
            });
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }
}
