using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using NUnit.Framework;
using PhialeGrid.Core;
using PhialeGrid.Core.Layout;
using PhialeTech.PhialeGrid.Wpf.Surface;
using UniversalInput.Contracts;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridSurfaceHostUniversalInputTests
    {
        [Test]
        public void Initialize_WhenCoordinatorPublishesSnapshot_RendersSurfacePanel()
        {
            var coordinator = CreateCoordinator();
            var host = CreateHost();
            var window = CreateWindow(host);

            try
            {
                host.Initialize(coordinator);
                window.Show();
                FlushDispatcher(host.Dispatcher);

                Assert.That(host.SurfacePanelForTesting.Children.Count, Is.GreaterThan(0));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void HandleUniversalWheelInput_UpdatesCoordinatorViewport()
        {
            var coordinator = CreateScrollableCoordinator();
            var host = CreateHost();

            host.Initialize(coordinator);
            host.HandleWheelForTesting(new UniversalPointerWheelChangedEventArgs(80, new UniversalPoint { X = 50, Y = 40 }));

            Assert.That(coordinator.GetCurrentSnapshot().ViewportState.VerticalOffset, Is.EqualTo(80));
        }

        [Test]
        public void HandleUniversalPointerPressed_UpdatesCurrentCell()
        {
            var coordinator = CreateCoordinator();
            var host = CreateHost();

            host.Initialize(coordinator);
            host.HandlePointerPressedForTesting(new UniversalPointerRoutedEventArgs(
                new UniversalPointer(DeviceType.Mouse, new UniversalPoint { X = 50, Y = 40 })
                {
                    Properties = new UniversalPointerPointProperties { IsLeftButtonPressed = true },
                })
            {
                Metadata = new UniversalMetadata { ClickCount = 1 },
            });

            var currentCell = coordinator.GetCurrentSnapshot().CurrentCell;
            Assert.Multiple(() =>
            {
                Assert.That(currentCell, Is.Not.Null);
                Assert.That(currentCell.RowKey, Is.EqualTo("row-1"));
                Assert.That(currentCell.ColumnKey, Is.EqualTo("col-1"));
            });
        }

        [Test]
        public void Initialize_WhenCalledTwice_DoesNotDuplicateCoordinatorSubscriptions()
        {
            var coordinator = CreateCoordinator();
            var host = CreateHost();

            host.Initialize(coordinator);
            host.Initialize(coordinator);

            Assert.Multiple(() =>
            {
                Assert.That(GetSubscriberCount(coordinator, "SnapshotChanged"), Is.EqualTo(1));
                Assert.That(GetSubscriberCount(coordinator, "FocusRequested"), Is.EqualTo(1));
            });
        }

        private static GridSurfaceCoordinator CreateCoordinator()
        {
            var coordinator = new GridSurfaceCoordinator();
            coordinator.Initialize(
                new[] { new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 } },
                new[] { new GridRowDefinition { RowKey = "row-1", Height = 20 } });
            return coordinator;
        }

        private static GridSurfaceCoordinator CreateScrollableCoordinator()
        {
            var coordinator = new GridSurfaceCoordinator();
            coordinator.Initialize(
                new[]
                {
                    new GridColumnDefinition { ColumnKey = "col-1", Header = "Col 1", Width = 100 },
                    new GridColumnDefinition { ColumnKey = "col-2", Header = "Col 2", Width = 100 },
                },
                new[]
                {
                    new GridRowDefinition { RowKey = "row-1", Height = 40 },
                    new GridRowDefinition { RowKey = "row-2", Height = 40 },
                    new GridRowDefinition { RowKey = "row-3", Height = 40 },
                    new GridRowDefinition { RowKey = "row-4", Height = 40 },
                    new GridRowDefinition { RowKey = "row-5", Height = 40 },
                });
            coordinator.SetViewportSize(120, 120);
            return coordinator;
        }

        private static GridSurfaceHost CreateHost()
        {
            return new GridSurfaceHost
            {
                Width = 400,
                Height = 200,
            };
        }

        private static Window CreateWindow(GridSurfaceHost host)
        {
            return new Window
            {
                Width = 400,
                Height = 200,
                Content = host,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
            };
        }

        private static int GetSubscriberCount(GridSurfaceCoordinator coordinator, string eventName)
        {
            var field = typeof(GridSurfaceCoordinator).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            var handler = field.GetValue(coordinator) as Delegate;
            return handler?.GetInvocationList().Length ?? 0;
        }

        private static void FlushDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }
}
