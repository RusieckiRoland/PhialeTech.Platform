using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using NUnit.Framework;
using PhialeGrid.Core.Surface;
using PhialeTech.Components.Shared.ViewModels;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeGrid.Wpf.Tests.Surface;
using WpfGrid = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

namespace PhialeGrid.Wpf.Tests.Selection
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public sealed class GridRowHeaderSelectionBehaviorTests
    {
        [Test]
        public void SelectingWholeRowAsCells_ShouldKeepRowHeaderIndependentFromRecordSelection()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            var grid = CreateBoundDemoGrid(viewModel);
            var window = CreateHostWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                viewModel.SelectExample("selection");
                FlushDispatcher(grid.Dispatcher);

                var surfaceHost = (GridSurfaceHost)grid.FindName("SurfaceHost");
                Assert.That(surfaceHost, Is.Not.Null);

                var initialSnapshot = surfaceHost.CurrentSnapshot;
                var rowHeaders = initialSnapshot.Headers
                    .Where(header => header.Kind == GridHeaderKind.RowHeader)
                    .Take(2)
                    .ToArray();
                Assert.That(rowHeaders.Length, Is.EqualTo(2));

                ClickSurfaceRowHeader(surfaceHost, rowHeaders[0]);
                FlushDispatcher(grid.Dispatcher);

                var updatedSnapshot = surfaceHost.CurrentSnapshot;
                var selectedHeader = updatedSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == rowHeaders[0].HeaderKey);
                var unselectedHeader = updatedSnapshot.Headers.Single(header => header.Kind == GridHeaderKind.RowHeader && header.HeaderKey == rowHeaders[1].HeaderKey);
                var selectedRow = updatedSnapshot.Rows.Single(row => row.RowKey == rowHeaders[0].HeaderKey);
                var unselectedRow = updatedSnapshot.Rows.Single(row => row.RowKey == rowHeaders[1].HeaderKey);

                Assert.Multiple(() =>
                {
                    Assert.That(selectedHeader.IsSelected, Is.False);
                    Assert.That(unselectedHeader.IsSelected, Is.False);
                    Assert.That(selectedRow.IsSelected, Is.False);
                    Assert.That(unselectedRow.IsSelected, Is.False);
                    Assert.That(grid.SelectionStatusText, Does.Contain("1"));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RowStateColumn_WhenAllUtilityFeaturesAreDisabled_RemainsVisibleAsDedicatedEmptyStateColumn()
        {
            var viewModel = new DemoShellViewModel("Wpf");
            viewModel.SelectExample("selection");
            var grid = CreateBoundDemoGrid(viewModel);
            grid.SelectCurrentRow = false;
            grid.MultiSelect = false;
            grid.ShowNb = false;
            grid.ShowCurrentRecordIndicator = false;
            var window = CreateHostWindow(grid);

            try
            {
                window.Show();
                FlushDispatcher(grid.Dispatcher);

                var surfaceHost = (GridSurfaceHost)grid.FindName("SurfaceHost");
                Assert.That(surfaceHost, Is.Not.Null);
                var firstRowHeader = surfaceHost.CurrentSnapshot.Headers
                    .FirstOrDefault(header => header.Kind == GridHeaderKind.RowHeader);
                var indicatorSlot = firstRowHeader == null
                    ? null
                    : GridSurfaceTestHost.FindElementByAutomationId<FrameworkElement>(surfaceHost, "surface.row-indicator." + firstRowHeader.HeaderKey);

                Assert.Multiple(() =>
                {
                    Assert.That(grid.ResolvedRowIndicatorWidth, Is.EqualTo(20d).Within(0.1d));
                    Assert.That(grid.ResolvedRowStateWidth, Is.EqualTo(20d).Within(0.1d));
                    Assert.That(grid.ResolvedRowHeaderWidth, Is.EqualTo(20d).Within(0.1d));
                    Assert.That(surfaceHost.CurrentSnapshot.Headers.Any(header => header.Kind == GridHeaderKind.RowHeader), Is.True);
                    Assert.That(surfaceHost.CurrentSnapshot.Headers.Any(header => header.Kind == GridHeaderKind.RowNumberHeader), Is.False);
                    Assert.That(firstRowHeader, Is.Not.Null);
                    Assert.That(firstRowHeader.ShowRowIndicator, Is.False);
                    Assert.That(firstRowHeader.Bounds.Width, Is.EqualTo(grid.ResolvedRowStateWidth).Within(1d));
                    Assert.That(indicatorSlot, Is.Not.Null);
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static void ClickSurfaceRowHeader(GridSurfaceHost surfaceHost, GridHeaderSurfaceItem rowHeader)
        {
            var x = rowHeader.Bounds.X + (rowHeader.Bounds.Width / 2d);
            var y = rowHeader.Bounds.Y + (rowHeader.Bounds.Height / 2d);
            GridSurfaceTestHost.ClickPointViaRoutedUi(surfaceHost, x, y);
        }

        private static WpfGrid CreateBoundDemoGrid(DemoShellViewModel viewModel)
        {
            var grid = new WpfGrid
            {
                Width = 1200,
                Height = 700,
                DataContext = viewModel,
                LanguageDirectory = global::PhialeGrid.Wpf.Tests.GridTestRepositoryPaths.GridLanguagesDirectory
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

        private static Window CreateHostWindow(FrameworkElement content)
        {
            return GridSurfaceTestHost.CreateHostWindow(content, width: 1200, height: 700);
        }

        private static void FlushDispatcher(System.Windows.Threading.Dispatcher dispatcher)
        {
            GridSurfaceTestHost.FlushDispatcher(dispatcher);
        }
    }
}




