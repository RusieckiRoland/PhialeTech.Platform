using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using NUnit.Framework;
using PhialeGrid.Core.Rendering;
using PhialeGrid.Core.Surface;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeGrid.Wpf.Tests.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Selection
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridTouchSelectionBehaviorTests
    {
        [Test]
        public void TouchInteractionMode_ShouldUseWholeRowCellSelectionAndShowRowSelectors()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("selection");

            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.InteractionMode = GridInteractionMode.Touch;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var firstRowHeader = surfaceHost.CurrentSnapshot.Headers.First(header => header.Kind == GridHeaderKind.RowHeader);

                GridSurfaceTestHost.ClickBoundsCenter(surfaceHost, firstRowHeader.Bounds);
                GridSurfaceTestHost.FlushDispatcher(grid);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;
                var selectedHeader = updatedSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == firstRowHeader.HeaderKey);
                var selectedRow = updatedSnapshot.Rows.Single(row => row.RowKey == firstRowHeader.HeaderKey);
                var selectedCells = updatedSnapshot.Cells.Where(cell => cell.RowKey == firstRowHeader.HeaderKey).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(grid.ShowsTouchRowSelectors, Is.True);
                    Assert.That(grid.ResolvedSelectionMode, Is.EqualTo(GridSelectionMode.Cell));
                    Assert.That(firstRowHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowHeaderWidth).Within(1d));
                    Assert.That(selectedHeader.IsSelected, Is.False);
                    Assert.That(selectedRow.IsSelected, Is.False);
                    Assert.That(selectedCells.All(cell => cell.IsSelected), Is.True);
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SelectVisibleRowsAndClearSelection_ShouldKeepTouchSelectionPredictable()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("selection");

            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                grid.InteractionMode = GridInteractionMode.Touch;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);

                var visibleDataRows = surfaceHost.CurrentSnapshot.Headers
                    .Count(header => header.Kind == GridHeaderKind.RowHeader);
                var selectableRows = surfaceHost.CurrentSnapshot.Rows
                    .Count(row => !row.IsGroupHeader && !row.IsDetailsHost && !row.IsLoadMore);

                grid.SelectVisibleRows();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var selectedSnapshot = surfaceHost.CurrentSnapshot;
                var selectedRowCount = selectedSnapshot.Rows.Count(row => row.IsSelected);
                var selectedHeaderCount = selectedSnapshot.Headers.Count(header => header.Kind == GridHeaderKind.RowHeader && header.IsSelected);
                var selectedCellCount = selectedSnapshot.Cells.Count(cell => cell.IsSelected);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasSelectedRows, Is.True);
                    Assert.That(selectedRowCount, Is.EqualTo(0));
                    Assert.That(selectedHeaderCount, Is.EqualTo(0));
                    Assert.That(selectedCellCount, Is.GreaterThan(0));
                    Assert.That(grid.SelectionStatusText, Does.Contain(selectableRows.ToString()));
                });

                grid.ClearSelection();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var clearedSnapshot = surfaceHost.CurrentSnapshot;

                Assert.Multiple(() =>
                {
                    Assert.That(grid.HasSelectedRows, Is.False);
                    Assert.That(clearedSnapshot.Rows.Any(row => row.IsSelected), Is.False);
                    Assert.That(clearedSnapshot.Headers.Any(header => header.Kind == GridHeaderKind.RowHeader && header.IsSelected), Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("0"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DensityChange_ShouldUpdateSurfaceRowMetricsAndRestoreThemWhenSwitchedBack()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("selection");

            var grid = CreateBoundDemoGrid(viewModel);
            var window = GridSurfaceTestHost.CreateHostWindow(grid, width: 1200, height: 720);

            try
            {
                window.Show();
                GridSurfaceTestHost.FlushDispatcher(grid);

                var surfaceHost = GridSurfaceTestHost.FindSurfaceHost(grid);
                var compactSnapshot = surfaceHost.CurrentSnapshot;
                var compactRow = compactSnapshot.Rows.First(row => !row.IsDummy && !row.IsGroupHeader && !row.IsDetailsHost);

                grid.Density = GridDensity.Touch;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var touchSnapshot = surfaceHost.CurrentSnapshot;
                var touchRow = touchSnapshot.Rows.First(row => !row.IsDummy && !row.IsGroupHeader && !row.IsDetailsHost);
                var touchPadding = (Thickness)grid.Resources["PgGridCellPadding"];

                grid.Density = GridDensity.Compact;
                GridSurfaceTestHost.FlushDispatcher(grid);

                var compactAgainSnapshot = surfaceHost.CurrentSnapshot;
                var compactAgainRow = compactAgainSnapshot.Rows.First(row => !row.IsDummy && !row.IsGroupHeader && !row.IsDetailsHost);
                var compactAgainPadding = (Thickness)grid.Resources["PgGridCellPadding"];

                Assert.Multiple(() =>
                {
                    Assert.That(compactRow.Height, Is.EqualTo(30d));
                    Assert.That(compactSnapshot.ViewportState.Metrics.RowHeaderWidth, Is.EqualTo(20d));
                    Assert.That(touchRow.Height, Is.EqualTo(46d));
                    Assert.That(touchSnapshot.ViewportState.Metrics.RowHeaderWidth, Is.EqualTo(24d));
                    Assert.That(touchPadding, Is.EqualTo(new Thickness(12d, 10d, 12d, 10d)));
                    Assert.That(compactAgainRow.Height, Is.EqualTo(30d));
                    Assert.That(compactAgainSnapshot.ViewportState.Metrics.RowHeaderWidth, Is.EqualTo(20d));
                    Assert.That(compactAgainPadding, Is.EqualTo(new Thickness(8d, 4d, 8d, 4d)));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1200,
                Height = 720,
                DataContext = viewModel,
                LanguageDirectory = GetLanguageDirectory(),
            };

            BindingOperations.SetBinding(grid, WpfGrid.ItemsSourceProperty, new Binding(nameof(DemoShellViewModel.GridRecords)));
            BindingOperations.SetBinding(grid, WpfGrid.ColumnsProperty, new Binding(nameof(DemoShellViewModel.GridColumns)));
            BindingOperations.SetBinding(grid, WpfGrid.EditSessionContextProperty, new Binding(nameof(DemoShellViewModel.GridEditSessionContext)));
            BindingOperations.SetBinding(grid, WpfGrid.GroupsProperty, new Binding(nameof(DemoShellViewModel.GridGroups)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SortsProperty, new Binding(nameof(DemoShellViewModel.GridSorts)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, WpfGrid.SummariesProperty, new Binding(nameof(DemoShellViewModel.GridSummaries)));
            BindingOperations.SetBinding(grid, WpfGrid.IsGridReadOnlyProperty, new Binding(nameof(DemoShellViewModel.IsGridReadOnly)));
            return grid;
        }

        private static string GetLanguageDirectory()
        {
            return global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory;
        }
    }
}
