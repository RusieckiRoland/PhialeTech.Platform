using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.State;
using PhialeGrid.Core.Rendering;
using PhialeTech.Components.Shared.Services;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.Components.Wpf;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Surface
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public sealed class ApplicationStateManagerIntegrationTests
    {
        [Test]
        public void MainWindow_WhenReopenedWithSharedServices_RestoresGridViewStateForSameExampleKey()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "PhialeTech.Components.StateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            using var services = DemoApplicationServices.CreateDefault("PhialeTech.Components.Tests", rootDirectory);

            try
            {
                var firstWindow = new MainWindow(services);
                try
                {
                    firstWindow.Show();
                    GridSurfaceTestHost.FlushDispatcher(firstWindow);

                    var firstViewModel = (DemoShellViewModel)firstWindow.DataContext;
                    var firstGrid = FindDemoGrid(firstWindow);
                    firstViewModel.SelectExample("grouping");
                    GridSurfaceTestHost.FlushDispatcher(firstWindow);

                    firstGrid.SelectCurrentRow = false;
                    firstGrid.MultiSelect = true;
                    firstGrid.ShowNb = true;
                    firstGrid.RowNumberingMode = GridRowNumberingMode.WithinGroup;
                    firstGrid.SetColumnVisibility("District", false);
                    firstGrid.ApplyGlobalSearch("Krakow");
                    GridSurfaceTestHost.FlushDispatcher(firstWindow);
                }
                finally
                {
                    firstWindow.Close();
                }

                var secondWindow = new MainWindow(services);
                try
                {
                    secondWindow.Show();
                    GridSurfaceTestHost.FlushDispatcher(secondWindow);

                    var secondViewModel = (DemoShellViewModel)secondWindow.DataContext;
                    var secondGrid = FindDemoGrid(secondWindow);
                    secondViewModel.SelectExample("grouping");
                    GridSurfaceTestHost.FlushDispatcher(secondWindow);

                    Assert.Multiple(() =>
                    {
                        Assert.That(secondGrid.SelectCurrentRow, Is.False);
                        Assert.That(secondGrid.MultiSelect, Is.True);
                        Assert.That(secondGrid.ShowNb, Is.True);
                        Assert.That(secondGrid.RowNumberingMode, Is.EqualTo(GridRowNumberingMode.WithinGroup));
                        Assert.That(secondGrid.VisibleColumns.Any(column => string.Equals(column.ColumnId, "District", StringComparison.OrdinalIgnoreCase)), Is.False);
                        Assert.That(secondGrid.GlobalSearchText, Is.EqualTo("Krakow"));
                        Assert.That(secondViewModel.GridSearchText, Is.EqualTo("Krakow"));
                    });
                }
                finally
                {
                    secondWindow.Close();
                }
            }
            finally
            {
                if (Directory.Exists(rootDirectory))
                {
                    Directory.Delete(rootDirectory, true);
                }
            }
        }

        [Test]
        public void MainWindow_WhenReopenedForEditing_ReappliesDemoEditedAndInvalidMarkersAfterRestore()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "PhialeTech.Components.StateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            using var services = DemoApplicationServices.CreateDefault("PhialeTech.Components.Tests", rootDirectory);

            try
            {
                var firstWindow = new MainWindow(services);
                try
                {
                    firstWindow.Show();
                    GridSurfaceTestHost.FlushDispatcher(firstWindow);

                    var firstViewModel = (DemoShellViewModel)firstWindow.DataContext;
                    firstViewModel.SelectExample("editing");
                    GridSurfaceTestHost.FlushDispatcher(firstWindow);
                }
                finally
                {
                    firstWindow.Close();
                }

                var secondWindow = new MainWindow(services);
                try
                {
                    secondWindow.Show();
                    GridSurfaceTestHost.FlushDispatcher(secondWindow);

                    var secondViewModel = (DemoShellViewModel)secondWindow.DataContext;
                    var secondGrid = FindDemoGrid(secondWindow);
                    secondViewModel.SelectExample("editing");
                    GridSurfaceTestHost.FlushDispatcher(secondWindow);

                    Assert.Multiple(() =>
                    {
                        Assert.That(secondGrid.PendingEditRowIds, Does.Contain("DZ-KRA-STA-0001"));
                        Assert.That(secondGrid.ValidationIssueRowIds, Does.Contain("BLD-WRO-FAB-0002"));
                        Assert.That(secondGrid.PendingEditCount, Is.GreaterThanOrEqualTo(2));
                        Assert.That(secondGrid.HasValidationIssues, Is.True);
                    });
                }
                finally
                {
                    secondWindow.Close();
                }
            }
            finally
            {
                if (Directory.Exists(rootDirectory))
                {
                    Directory.Delete(rootDirectory, true);
                }
            }
        }

        [Test]
        public void MainWindow_WhenPersistedGridStateIsInvalid_DoesNotCrashAndLoadsExampleBaseline()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "PhialeTech.Components.StateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            using var services = DemoApplicationServices.CreateDefault("PhialeTech.Components.Tests", rootDirectory);

            try
            {
                services.ApplicationStateManager.Save(
                    "Demo/Grid/grouping",
                    new GridViewState
                    {
                        Version = 4,
                        RegionLayout =
                        {
                            new GridViewRegionState
                            {
                                RegionKind = GridRegionKind.SideToolRegion,
                                State = GridRegionState.Collapsed,
                                IsActive = true,
                                Size = 320d,
                            },
                        },
                    });

                var window = new MainWindow(services);
                try
                {
                    window.Show();
                    GridSurfaceTestHost.FlushDispatcher(window);

                    var viewModel = (DemoShellViewModel)window.DataContext;
                    var grid = FindDemoGrid(window);
                    viewModel.SelectExample("grouping");
                    GridSurfaceTestHost.FlushDispatcher(window);

                    Assert.Multiple(() =>
                    {
                        Assert.That(window.IsLoaded, Is.True);
                        Assert.That(grid.VisibleColumns.Count, Is.GreaterThan(0));
                        Assert.That(viewModel.SelectedExample?.Id, Is.EqualTo("grouping"));
                    });
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                if (Directory.Exists(rootDirectory))
                {
                    Directory.Delete(rootDirectory, true);
                }
            }
        }

        private static WpfGrid FindDemoGrid(MainWindow window)
        {
            var grid = (WpfGrid)window.FindName("DemoGrid");
            Assert.That(grid, Is.Not.Null);
            return grid;
        }
    }
}

